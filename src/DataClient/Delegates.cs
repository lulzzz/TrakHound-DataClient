// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE', which is part of this source code package.

using System.Collections.Generic;
using TrakHound.Api.v2.Streams.Data;

namespace TrakHound.DataClient
{
    public delegate void AgentDefinitionsHandler(AgentDefinitionData definition);

    public delegate void ComponentDefinitionsHandler(List<ComponentDefinitionData> definitions);

    public delegate void DataItemDefinitionsHandler(List<DataItemDefinitionData> definitions);

    public delegate void DeviceDefinitionsHandler(DeviceDefinitionData definition);

    public delegate void SamplesHandler(List<SampleData> samples);

    public delegate void StatusHandler(StatusData status);
}
