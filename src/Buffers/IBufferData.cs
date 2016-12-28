// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.DataClient.Buffers
{
    /// <summary>
    /// Interface for data stored in a Buffer queue
    /// </summary>
    public interface IBufferData
    {
        string EntryId { get; set; }
    }
}
