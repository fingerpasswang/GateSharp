using System;
using System.Collections.Generic;

namespace Gate
{
    class ForwardManager
    {
        private readonly Dictionary<int, HashSet<Guid>> forwardIdToClients = new Dictionary<int, HashSet<Guid>>();

        public void AddForward(Guid remote, int forwardId)
        {
            HashSet<Guid> remotes = null;
            if (!forwardIdToClients.TryGetValue(forwardId, out remotes))
            {
                remotes = forwardIdToClients[forwardId] = new HashSet<Guid>();
            }

            remotes.Add(remote);
        }

        public void RemoveForward(Guid remote, int forwardId)
        {
            HashSet<Guid> remotes = null;
            if (!forwardIdToClients.TryGetValue(forwardId, out remotes))
            {
                return;
            }

            remotes.Remove(remote);
        }

        public IEnumerable<Guid> GetForwardGroup(int forwardId)
        {
            HashSet<Guid> remotes = null;
            if (!forwardIdToClients.TryGetValue(forwardId, out remotes))
            {
                yield break;
            }

            foreach (var remote in remotes)
            {
                yield return remote;
            }
        }
    }
}
