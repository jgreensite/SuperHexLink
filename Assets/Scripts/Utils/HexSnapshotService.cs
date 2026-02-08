using Sirenix.Serialization;

namespace SuperHexLink.Utils
{
    public class HexSnapshotService : IHexSnapshotService
    {
        public byte[] CreateSnapshot<T>(T state)
        {
            if (state == null) return null;
            return SerializationUtility.SerializeValue(state, DataFormat.JSON);
        }

        public T RestoreSnapshot<T>(byte[] snapshot)
        {
            if (snapshot == null || snapshot.Length == 0) return default;
            return SerializationUtility.DeserializeValue<T>(snapshot, DataFormat.JSON);
        }
    }
}
