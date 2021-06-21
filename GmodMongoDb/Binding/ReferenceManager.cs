using GmodNET.API;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace GmodMongoDb.Binding
{
    public static class ReferenceManager
    {
        private static List<IDisposable>? disposables = null;
        private static List<GCHandle>? handles = null;

        public static void Add(GCHandle handle)
        {
            if (handles == null)
                handles = new List<GCHandle>();

            handles.Add(handle);
        }

        public static void Add(IDisposable disposable)
        {
            if (disposables == null)
                disposables = new List<IDisposable>();

            disposables.Add(disposable);
        }

        public static void KillAll(ILua _)
        {
            if(handles != null)
            {
                foreach (var handle in handles)
                {
                    if(handle.IsAllocated)
                        handle.Free();
                }

                handles.Clear();
                handles = null;
            }

            if(disposables != null)
            {

                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }

                disposables.Clear();
                disposables = null;
            }
        }
    }
}
