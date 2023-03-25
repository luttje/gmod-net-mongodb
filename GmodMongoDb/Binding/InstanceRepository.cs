using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GmodMongoDb.Binding
{
    internal class InstanceRepository
    {
        public static InstanceRepository Instance { get; } = new InstanceRepository();
        
        private static readonly Dictionary<string, nint> InstanceIds = new();

        private InstanceRepository() { }
        
        public string RegisterInstance(object instance)
        {
            var pointer = GCHandle.ToIntPtr(GCHandle.Alloc(instance));
            var instanceId = Guid.NewGuid().ToString();

            InstanceIds.Add(instanceId, pointer);

            return instanceId;
        }

        public nint? GetInstance(string instanceId)
        {
            if (InstanceIds.TryGetValue(instanceId, out var pointer))
            {
                return pointer;
            }

            return null;
        }
    }
}
