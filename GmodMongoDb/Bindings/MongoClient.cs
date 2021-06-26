using GmodMongoDb.Binding;
using GmodNET.API;
using MongoDB.Driver;
using System;
using System.Net.Sockets;
using System.Reflection;

namespace GmodMongoDb.Bindings
{
    class MongoClient : LuaBinding, IDisposable
    {
        private MongoDB.Driver.MongoClient Instance;

        public MongoClient()
            :base(typeof(MongoDB.Driver.MongoClient))
        {
            // Ensure the sockets are cleaned up
            ReferenceManager.Add(this);
        }

        public override ConstructorInfo PreConstructorInvoke(ILua lua, ConstructorInfo constructor, ref object[] parameters)
        {
            constructor = base.PreConstructorInvoke(lua, constructor, ref parameters);

            var parameter = parameters[0];

            MongoClientSettings? settings = null;

            if (parameter is MongoClientSettings clientSettings)
                settings = clientSettings;

            if (parameter is MongoUrl url)
                settings = MongoClientSettings.FromUrl(url);

            if (parameter is string urlString)
                settings = MongoClientSettings.FromUrl(new MongoUrl(urlString));

            settings.HeartbeatInterval = TimeSpan.FromSeconds(1);
            // TODO: Support existing settings.ClusterConfigurator in settings
            settings.ClusterConfigurator = builder =>
            {
                // A lingering socket causes the module to become not-unloadable.
                static void SocketConfigurator(Socket s)
                {
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                };

                builder.ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>)SocketConfigurator));
            };

            parameters[0] = settings;
            return constructor.DeclaringType.GetConstructor(new Type[] { typeof(MongoClientSettings) });
        }

        public override object PostConstructorInvoke(ILua lua, ConstructorInfo constructor, object instance)
        {
            Instance = (MongoDB.Driver.MongoClient)instance;

            return base.PostConstructorInvoke(lua, constructor, instance);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (Instance == null)
                return;

            ClusterRegistry.Instance.UnregisterAndDisposeCluster(Instance.Cluster);
        }
    }
}
