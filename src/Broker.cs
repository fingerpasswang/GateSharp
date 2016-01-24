using System;
using System.Collections.Generic;
using Network;

namespace Gate
{
    class Broker
    {
        private readonly Gate gate;

        private readonly Dictionary<int, Dictionary<Guid, IRemote>> keyTobackendSubcriptions = new Dictionary<int, Dictionary<Guid, IRemote>>();

        public void RouteData(int key, Guid srcUuid, byte[] data, int len, int offset)
        {
            Dictionary<Guid, IRemote> subcriptions = null;
            if (!keyTobackendSubcriptions.TryGetValue(key, out subcriptions))
            {
                return;
            }

            IRemote backend = null;
            if (!subcriptions.TryGetValue(srcUuid, out backend))
            {
                return;
            }

            Gate.PushReceivedMessage(backend, data, len, offset);
        }

        public void AddRouting(int key, Guid srcUuid, IRemote routeTo)
        {
            Dictionary<Guid, IRemote> subcriptions = null;
            if (!keyTobackendSubcriptions.TryGetValue(key, out subcriptions))
            {
                subcriptions = keyTobackendSubcriptions[key] = new Dictionary<Guid, IRemote>();
            }

            subcriptions[srcUuid] = routeTo;
        }
    }
}
