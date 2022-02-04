namespace Glazer.Blockchains.Models.Interfaces
{
    public interface IPacketCallback
    {
        /// <summary>
        /// Called when the instance is unpacked from the packet.
        /// </summary>
        void OnUnpacked();

        /// <summary>
        /// Called when the instance is packed into the packet.
        /// </summary>
        void OnPacking();
    }
}
