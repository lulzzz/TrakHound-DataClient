// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Collections.Generic;
using TrakHound.DataClient.Data;

namespace TrakHound.DataClient
{
    public delegate void AgentDefinitionsHandler(AgentDefinition definition);

    public delegate void ComponentDefinitionsHandler(List<ComponentDefinition> definitions);

    public delegate void DataItemDefinitionsHandler(List<DataItemDefinition> definitions);

    public delegate void DeviceDefinitionsHandler(DeviceDefinition definition);

    public delegate void SamplesHandler(List<Sample> samples);
}
