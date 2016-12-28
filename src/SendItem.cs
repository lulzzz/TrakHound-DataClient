// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace TrakHound.DataClient
{
    public enum SendItemType
    {
        SAMPLE,
        DATA_DEFINITION,
        CONTAINER_DEFINITION
    }

    public interface ISendItem
    {
        string Uuid { get; set; }

        SendItemType ItemType { get; }

        string ToCsv();
    }

}
