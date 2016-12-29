![TrakHound DataClient](dataclient-logo-100px.png)
<br>
<br>
TrakHound DataClient reads MTConnect® streams and sends the data to TrakHound DataServers to be stored. 

TrakHound DataClient and DataServer are designed specifically to store MTConnect® data in a database. Nearly all MTConnect data is stored with its original terminology in database tables for data storage or to use with cloud applications. 

![TrakHound DataClients and DataServers](DataClient-Server-01.jpg)

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


# Configuration
Configuration is read from the **server.conf** XML file in the following format:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<DataClient>

  <!--List of configured MTConnect Devices to read from-->
  <Devices>
    <Device deviceId="1234" deviceName="VMC-3Axis">http://agent.mtconnect.org</Device>
  </Devices>

  <!--Configuration for finding MTConnect Devices on the network-->
  <DeviceFinder scanInterval="5000">
    
    <!--Specify Port Range-->
    <Ports minimum="5000" maximum="5020"/>

    <!--Specify Address Range-->
    <Addresses minimum="192.168.1.100" maximum="192.168.1.120"/>
    
  </DeviceFinder>
    
  <!--Configuration for sending data to TrakHound DataServers-->
  <DataServers>
    <DataServer hostname="192.168.1.15" useSSL="true">
      
      <!--Data Buffer Directory to buffer failed transfers until sent successfully-->
      <Buffer>c:\TrakHound\Buffers\</Buffer>
      
      <!--Define the data to send to DataServer-->
      <DataGroups>

        <!--Collect all data-->
        <DataGroup name="all" captureMode="ACTIVE">
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

#### Device ID 
###### *(XmlAttribute : deviceId)*
The globally unique identifier for the device. When detected automatically, the Device ID is a hash of the device's DeviceName, port, and MAC address. The MAC address is used so that MTConnect Agents can use DHCP while still being identified as the same device.

#### Device Name
###### *(XmlAttribute : deviceName)*
The DeviceName of the MTConnect Device to read from

#### Address
###### *(XmlText)*
The base Url of the MTConnect Agent. Do not specify the Device Name in the url, instead specify it under the deviceName attribute.

## Data Server
Represents each TrakHound Data Server that data is sent to in order to be strored and processed.

#### Url 
###### *(XmlAttribute : url)*
The base Url of the TrakHound Data Server to send data to

#### Buffer Path
###### *(XmlAttribute : bufferPath)*
The directory where the buffer files should be stored. The Buffer is used to store data that hasn't been successfully sent yet.


### DataGroups
DataGroups allow configuration for what data is captured and sent to the DataServer. Data is filtered by Type or by their parent container type. DataGroups can include a list for allowed types as well as for denied types. A CaptureMode can also be defined to configure when the data in the DataGroup is sent.

#### Name
The identifier for the DataGroup. This is primarily used when the DataGroup is being included in another group.

#### CaptureMode
The mode in when data is captured.
  - ACTIVE : Always capture and send data defined in the DataGroup
  - PASSIVE : Only capture and send when included in another DataGroup. This can be used for constantly changing data such as Axis Position to reduce the amount of data stored in the DataServer's database.
  
#### Allow
A list of Types and Ids to capture. This can also include container paths with the wildcard character (*) to allow any types or ids within the container.

- ID : DataItem/Component/Device Id to allow (case sensitive)
- TYPE : DataItem/Component/Device Type to allow. Can be in the format of "PATH_FEEDRATE" or "PathFeedrate".

#### Deny
A list of Types to not capture. This overrides any allowed types.

#### Include
A list of other DataGroups to include when capturing for the current DataGroup. For example, this can be used to capture position data only when another group changes in order to reduce the amount of data stored in the DataServer's database.










