namespace SuperHexLink.Utils
{
    public interface IHexSnapshotService
    {
        byte[] CreateSnapshot<T>(T state);
        T RestoreSnapshot<T>(byte[] snapshot);
    }
}
