using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

#nullable enable
namespace GmodMongoDb.Binding
{
    /// <summary>
    /// Helps keep track of handles and disposables. Call <see cref="ReferenceManager.KillAll()"/> to clean up any active handles.
    /// </summary>
    public static class ReferenceManager
    {
        private static List<IDisposable>? disposables = null;
        private static List<GCHandle>? handles = null;

        /// <summary>
        /// Registers a handle so it can be cleaned up with <see cref="ReferenceManager.KillAll()"/> later.
        /// </summary>
        /// <param name="handle">The handle to register</param>
        public static void Add(GCHandle handle)
        {
            if (handles == null)
                handles = new List<GCHandle>();

            handles.Add(handle);
        }

        /// <summary>
        /// Registers a disposable so it can be cleaned up with <see cref="ReferenceManager.KillAll()"/> later.
        /// </summary>
        /// <param name="disposable">The disposable object to register</param>
        public static void Add(IDisposable disposable)
        {
            if (disposables == null)
                disposables = new List<IDisposable>();

            disposables.Add(disposable);
        }

        /// <summary>
        /// Dispose and free all registered disposable objects and handles respectively.
        /// </summary>
        public static void KillAll()
        {
            if (handles != null)
            {
                foreach (var handle in handles)
                {
                    if(handle.IsAllocated)
                        handle.Free();
                }

                handles.Clear();
                handles = null;
            }

            if (disposables != null)
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
#nullable disable