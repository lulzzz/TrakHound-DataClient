![TrakHound DataClient](dataclient-logo-02-75px.png)
<br>
<br>
TrakHound DataClient reads MTConnect® streams and sends the data to TrakHound DataServers to be stored. 

TrakHound DataClient and DataServer are designed specifically to store MTConnect® data in a database. Nearly all MTConnect data is stored with its original terminology in database tables for data storage or to use with cloud applications. 

![TrakHound DataClients and DataServers](DataClient-Server-02.jpg)

# TrakHound
The TrakHound DataClient and DataServer applications provide the manufacturing community with a Free and Open Source alternative so anyone can start collecting valuable machine data that can be used to analyze and improve future production in the upcoming years that will dominated by the IIoT. TrakHound provides you the tools to collect MTConnect data in near raw form and to store that data for later use. Even if you don't see the need for this data now, you may in several years and will wish you had previous year's data to compare. **Prepare for tomorrow by getting started with TrakHound today!**

# Features
- Automatically finds and configures MTConnect devices on a network
- Data filtering with triggers to collect all data or only what is needed
- Ability to send data to multiple TrakHound DataServers to create data redundancy or to meet data security requirements (local vs cloud)
- Utitlizes streaming connections for both MTConnect and connections to TrakHound DataServers
- Supports SSL(TLS) for sending data to TrakHound DataServers
- Non-volatile buffering to retain collected data between connection interruptions


### Data Storage
**MTConnect Agents by themselves are not storage applications.** This is made clear in the MTConnect Standard. Instead the purpose of MTConnect Agents is to serve data to client applications when requested. While the Agent does keep a small buffer, this buffer is not intended to be used for data storage but rather to retain data between connection interruptions. TrakHound fulfills the role of requesting this data and then storing it in a database for permanant storage. Data is stored which can then be accessed by other TrakHound applications, ERP/MES systems, third party software, or by reading the database directly using software such as Microsoft Access.

### Cloud Applications
Although the MTConnect Agent is a server application itself, most situations require Incoming connections where the application accesses the Agent directly which requires firewall exceptions and since many Agents run on the machine contol itself this would mean each machine would need to accessible from outside networks (usually undesirable for security reasons). TrakHound solves this issue by centralizing the data onto a single server which can either be accessed using the TrakHound API over HTTP/HTTPS or directly to the database itself. Since all of the MTConnect data is now **Outgoing** as opposed to Incoming, machine controls can stay isolated from external networks while a single DataServer accepts incoming requests.

### Security
One of the main goals of TrakHound is to provide tools to securely collect data so that no matter what restrictions your industry requires, you can still benefit from data analysis to improve your manufacturing processes. TrakHound fully supports SSL(TLS) encrypted connections for the DataClient -> DataServer connections as well as the API access. When used with a trusted SSL Certificate, data is sent securely just as online banking/payments are sent. 

By centralizing the point where data is accessed, TrakHound also allows internal machine networks to stay isolated from external networks to prevent both unauthorized data access and possible viruses from effecting the machine controls themselves.

Each TrakHound DataClient can also filter data to only send specific data to certain DataServers. This can be used to only send status data to a cloud server used for machine status monitoring, while sending ALL data to a secure onsite server. 

Of course, the biggest security benefit to using TrakHound is that it is Open Source and the source code can be reviewed to insure exactly what data is being collected and to make sure that no other data is being sent anywhere it shouldn't be.


# Configuration
Configuration is read from the **client.conf** XML file in the following format:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<DataClient>

  <!--List of configured MTConnect Devices to read from-->
  <Devices>
    <Device deviceId="0307f8be-5be9-4c14-85a6-8a2a9c9223db" deviceName="VMC-3Axis">http://agent.mtconnect.org</Device>
  </Devices>

  <!--Configuration for finding MTConnect Devices on the network-->
  <DeviceFinder scanInterval="3600000">
    
    <!--Specify Port Range-->
    <Ports minimum="5000" maximum="5020"/>
    
  </DeviceFinder>
    
  <!--Configuration for sending data to TrakHound DataServers-->
  <DataServers>
    
    <DataServer hostname="192.168.1.15" port="8472" useSSL="true">
      
      <!--Data Buffer Directory to buffer failed transfers until sent successfully-->
      <Buffer path="Buffers"/>
      
      <!--Define the data to send to DataServer-->
      <DataGroups>

        <!--Ignore constantly changing samples-->
        <DataGroup name="monitor" captureMode="ARCHIVE">
          <Deny>
            <Filter>BLOCK</Filter>
            <Filter>LINE</Filter>
            <Filter>BLOCK</Filter>
            <Filter>POSITION</Filter>
            <Filter>PATH_FEEDRATE</Filter>
            <Filter>PATH_POSITION</Filter>
            <Filter>ROTARY_VELOCITY</Filter>
          </Deny>
          <Include>
            <DataGroup>snapshot</DataGroup>
          </Include>
        </DataGroup>

        <!--Take a snapshot of All dataitems-->
        <DataGroup name="snapshot" captureMode="INCLUDE">
          <Allow>
            <Filter>*</Filter>
          </Allow>
        </DataGroup>

      </DataGroups>

    </DataServer> 
    
  </DataServers>
  
</DataClient>
```

## Devices 
List of configured MTConnect Devices to read from.

```xml
 <Devices>
    <Device deviceId="0307f8be-5be9-4c14-85a6-8a2a9c9223db" deviceName="VMC-3Axis">http://agent.mtconnect.org</Device>
    <Device deviceId="TY3FNQCZKM3R2V0WWI9H3AUISLW" deviceName="Haas_Device">http://192.168.1.198:5000</Device>
    <Device deviceId="KGI13AOQSUERHF1XVQSFWLDIBS" deviceName="OKUMA.Lathe">http://192.168.1.198:5001</Device>
    <Device deviceId="RVJGKEA9ZXUPUIGFVMTQP98L0UY" deviceName="OKUMA.Lathe">http://192.168.1.198:5006</Device>
    <Device deviceId="UPSFKO6IBDAEOPSFEHLGAIMLCM" deviceName="OKUMA.MachiningCenter">http://192.168.1.198:5003</Device>
    <Device deviceId="UXJGZKSQS9DIEY7JQ912RS7Q4" deviceName="OKUMA.Grinder">http://192.168.1.198:5002</Device>
  </Devices>
  ```

#### Device ID 
###### *(XmlAttribute : deviceId)*
The globally unique identifier for the device. When detected automatically, the Device ID is a hash of the device's DeviceName, port, and MAC address. The MAC address is used so that MTConnect Agents can use DHCP while still being identified as the same device. If manually added, always use a GUID.

#### Device Name
###### *(XmlAttribute : deviceName)*
The DeviceName of the MTConnect Device to read from

#### Address
###### *(XmlText)*
The base Url of the MTConnect Agent. Do not specify the Device Name in the url, instead specify it under the deviceName attribute.


## DeviceFinder 
Configuration for finding MTConnect Devices on the network. *If omitted, the network will not be scanned and no devices will be automatically found or configured.*

```xml
 <DeviceFinder scanInterval="5000">
    
    <!--Specify Port Range-->
    <Ports minimum="5000" maximum="5020">
      <Allow>
        <Port>5120</Port>
      </Allow>
      <Deny>
        <Port>5002</Port>
        <Port>5003</Port>
        <Port>5007</Port> 
      </Deny>
    </Ports>

    <!--Specify Address Range-->    
    <Addresses minimum="192.168.1.100" maximum="192.168.1.120">
      <Allow>
        <Address>192.168.1.198</Address>    
      </Allow>
      <Deny>
        <Address>192.168.1.110</Address>
        <Address>192.168.1.102</Address>  
      </Deny>
    </Addresses>
    
  </DeviceFinder>
  ```
  
#### Scan Interval 
###### *(XmlAttribute : scanInterval)*
The interval (in milliseconds) at which the network will be scanned for new devices. *If omitted, the network will only be scanned when the DataClient is initially started.* *Note: If needed at all, it is recommended to keep this interval set high since the network will receive a Ping on all nodes which can lead to Anti-Virus/Security software flagging the application.*
  
### Ports
Used to filter the ports to scan. *If omitted, the default port range of 5000 - 5010 will be used.*

#### Minimum 
###### *(XmlAttribute : minimum)*
The minimum in the range of ports to search

#### Maximum 
###### *(XmlAttribute : maximum)*
The maximum in the range of ports to search

#### Allow
List of Ports that are specifically allowed to be searched. *Allowed ports override the range and denied ports.*

#### Denied
List of Ports that are specically denied and not allowed to be searched.

### Addresses
Used to filter the IP addresses to scan. *If omitted, all reachable IP addresses within the subnet will be scanned.*

#### Minimum 
###### *(XmlAttribute : minimum)*
The minimum in the range of addresses to search

#### Maximum 
###### *(XmlAttribute : maximum)*
The maximum in the range of addresses to search

#### Allow
List of Addresses that are specifically allowed to be searched. *Allowed addresses override the range and denied addresses.*

#### Denied
List of Addresses that are specically denied and not allowed to be searched.


## DataServers
Represents each TrakHound Data Server that data is sent to in order to be strored.

```xml
<DataServers>
    <DataServer hostname="192.168.1.15" port="8472" useSSL="true">
      
      <!--Data Buffer Directory to buffer failed transfers until sent successfully-->
      <Buffer>Buffers</Buffer>
      
      <!--Define the data to send to DataServer-->
      <DataGroups>

        <!--Ignore constantly changing samples-->
        <DataGroup name="monitor" captureMode="ARCHIVE">
          <Deny>
            <Filter>BLOCK</Filter>
            <Filter>LINE</Filter>
            <Filter>BLOCK</Filter>
            <Filter>POSITION</Filter>
            <Filter>PATH_FEEDRATE</Filter>
            <Filter>PATH_POSITION</Filter>
            <Filter>ROTARY_VELOCITY</Filter>
          </Deny>
          <Include>
            <DataGroup>snapshot</DataGroup>
          </Include>
        </DataGroup>

        <!--Take a snapshot of All dataitems-->
        <DataGroup name="snapshot" captureMode="INCLUDE">
          <Allow>
            <Filter>*</Filter>
          </Allow>
        </DataGroup>

      </DataGroups>

    </DataServer>
    
  </DataServers>
```

#### Hostname 
###### *(XmlAttribute : hostname)*
The hostname of the TrakHound Data Server to send data to

#### Port 
###### *(XmlAttribute : port)*
The port to send data to the TrakHound Data Server on. *If omitted, the default 8472 will be used.*

#### Use SSL (Secure Socket Layer) 
###### *(XmlAttribute : useSSL)*
The hostname of the TrakHound Data Server to send data to. *If omitted, the default of False will be used.*

#### Api Key 
###### *(XmlAttribute : apiKey)*
The Api Key used to authenticate when using the TrakHound Cloud DataServer. An ApiKey will be assigned to your user account when created at www.TrakHound.com.

### Buffer
Data Buffer Directory (relative or absolute path) to buffer failed transfers until sent successfully. *If omitted, no buffer will be used and may result in "lost" data.*


### DataGroups
DataGroups allow configuration for what data is captured and sent to the DataServer. Data is filtered by Type or by their parent container type. DataGroups can include a list for allowed types as well as for denied types. A CaptureMode can also be defined to configure when the data in the DataGroup is sent.

```xml
<DataGroups>
    <!--Collect ALL Data-->
    <DataGroup name="all" captureMode="ARCHIVE">
        <Allow>
        <Filter>*</Filter>
        </Allow>
    </DataGroup>
</DataGroups>
```

#### Name
The identifier for the DataGroup. This is primarily used when the DataGroup is being included in another group.

#### CaptureMode
The mode in when data is captured.
  - ARCHIVE : Capture all data and add to DataServer's archived_samples table (permanent storage)
  - CURRENT : Only capture the most current data. Similar to the MTConnect "Current" request. Can be used to minimize data storage space when only needing the current data. This data is stored in the DataServer's current_samples table.
  - INCLUDE : Only capture data when included in another DataGroup using the "Include" list (see below).
  
#### Allow
A list of Types and Ids to capture. This can also include container paths with the wildcard character (*) to allow any types or ids within the container.

- ID : DataItem/Component/Device Id to allow (case sensitive)
- TYPE : DataItem/Component/Device Type to allow. Can be in the format of "PATH_FEEDRATE" or "PathFeedrate".

#### Deny
A list of Types to not capture. This overrides any allowed types.

#### Include
A list of other DataGroups to include when capturing for the current DataGroup. For example, this can be used to capture position data only when another group changes in order to reduce the amount of data stored in the DataServer's database.

#### Filter
Filters specify the path to either allow or deny. Below are examples of accecptable syntax:

- ***** (All types/ids)

- **EmergencyStop** (Any DataItem with the type of EMERGENCY_STOP) *Note: either format can be used "EmergencyStop" or "EMERGENCY_STOP"

- **cn2** (Only the DataItem with the ID of "cn2")

- **Controller/*** (All types/ids WITHIN any component of type Controller)

- **Controller/Path/*** (All types/ids WITHIN any component of type Path that is also within a Controller component)

- **Controller/Path/cn3** (Only the DataItem with the ID of "cn3" that is within Path component that is also within a Controller component)

- **c1/*** (All types/ids WITHIN the component with the ID of "c1")

#### Examples

##### Example 1 (The "snapshot" technique)
Watch for when new "Status" data items are received within a Controller component that isn't listed under the Deny list. DataItems are denied that change constantly such as the current program line/block, feedrate, etc. When a new item is found, include the DataGroup "all". This essentially takes a snapshot of the entire device whenever one of the "Status" items is changed. 

```xml
<!--Ignore constantly changing samples-->
<DataGroup name="monitor" captureMode="ARCHIVE">

  <!--List the Denied Filters (Denied Filters override Allowed)-->
  <Deny>
	<Filter>BLOCK</Filter>
	<Filter>LINE</Filter>
	<Filter>BLOCK</Filter>
	<Filter>POSITION</Filter>
	<Filter>PATH_FEEDRATE</Filter>
	<Filter>PATH_POSITION</Filter>
	<Filter>ROTARY_VELOCITY</Filter>
  </Deny>
  
  <!--List the additional groups to include when current group is captured-->
  <Include>
	<DataGroup>snapshot</DataGroup>
  </Include>
  
</DataGroup>

<!--Take a snapshot of All dataitems-->
<DataGroup name="snapshot" captureMode="INCLUDE">

  <!--List the allowed Filters-->
  <Allow>
	<Filter>*</Filter>
  </Allow>
  
</DataGroup>
```

##### Example 2 (Only Current data)
Only collect the most current data. Can be used for 

```xml
<DataGroup name="current" captureMode="CURRENT">
    <Allow>
       <Filter>*</Filter>
    </Allow>
</DataGroup>
```

##### Example 3
Only collect data for a single DataItem with the type of "EXECUTION".

```xml
<DataGroup name="execution" captureMode="ARCHIVE">
    <Allow>
       <Filter>EXECUTION</Filter>
    </Allow>
</DataGroup>
```

##### Example 4
Only collect axis data.

```xml
<DataGroup name="axes" captureMode="ARCHIVE">
    <Allow>
       <Filter>Axes/*</Filter>
    </Allow>
</DataGroup>
```

##### Example 5
Only collect data when the DataItem of type EMEGENCY_STOP is changed. 

```xml
<DataGroup name="all" captureMode="INCLUDE">
    <Allow>
       <Filter>*</Filter>
    </Allow>
</DataGroup>

<DataGroup name="estop" captureMode="ARCHIVE">
    <Allow>
        <Filter>EmergencyStop</Filter>       
    </Allow>
    <Include>
        <DataGroup>all</DataGroup>
    </Include>
</DataGroup>
```

# Command Line Arguments

## Debug
### *(Argument = debug)*
Starting the DataClient with the 'debug' command line argument runs the DataClient as a **Console Application** instead of a Windows Service. This mode can be used for quickly debugging issues or for testing and development.

## Install
### *(Argument = install)*
Starting the DataClient with the 'install' command line argument installs the DataClient as a Windows Service.

## Uninstall
### *(Argument = uninstall)*
Starting the DataClient with the 'uninstall' command line argument uninstalls the DataClient's Windows Service.

