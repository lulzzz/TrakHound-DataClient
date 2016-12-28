// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.DataClient.DataGroups
{
    /// <summary>
    /// Type to define when to capture data
    /// </summary>
    public enum CaptureMode
    {
        /// <summary>
        /// Only capture data when included in another DataGroup
        /// </summary>
        PASSIVE,

        /// <summary>
        /// Always capture data
        /// </summary>
        ACTIVE
    }
}
