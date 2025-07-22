# BTHome Component Support for Shelly Exporters

## Overview

This implementation adds support for reading BTHome sensor components from Shelly devices. The feature is designed to be highly encapsulated and reusable across different Shelly device exporters.

## Architecture

### 1. Utilities.Components Library

**Location**: `Utilities/Components/`

**Key Classes**:
- `BtHomeDevice`: Represents a BTHome device with its associated sensors
- `BtHomeSensor`: Represents an individual sensor
- `BtHomeComponentsHandler`: Handles parsing and managing BTHome component data

**Design Principles**:
- **Separation of Concerns**: Device logic separated from sensor logic
- **Simplicity**: Direct concrete classes without unnecessary abstractions
- **Encapsulation**: All component logic contained within the Utilities library
- **User-Controlled Naming**: Relies on user-provided sensor names without type assumptions

### 2. Logical Structure

```
BtHomeDevice
├── Properties: Id, Name, Address, Rssi, Battery, LastUpdatedTimestamp  
└── Sensors: List<BtHomeSensor>
    └── BtHomeSensor
        ├── Properties: Id, Name, Value, LastUpdatedTimestamp, ObjectId, Index
        └── Method: GetLabels(deviceName)
```

### 3. Prometheus Metrics Integration

**New Metric Type**: `LabeledGaugeMetric` in `Utilities/Metrics/`
- Supports Prometheus labels for richer metrics
- Format: `devicename_component{device_name="DeviceName", sensor_name="SensorName"} value`

### 4. Configuration

Added `enableComponentMetrics` boolean to `TargetDevice` configuration:
```csharp
public bool enableComponentMetrics = false; // Set to true to enable BTHome component metrics
```

## Features

### Simultaneous API Requests
- Makes concurrent requests to both `/rpc/Shelly.GetStatus` and `/rpc/Shelly.GetComponents`
- Improves performance by reducing total request time

### Dynamic Metric Discovery
- Components are discovered dynamically at runtime
- No need to predefine component structure

### Device-Sensor Mapping Logic
1. Finds all `bthomedevice:*` entries with valid names
2. Maps their MAC addresses to device names
3. Finds `bthomesensor:*` entries with valid names
4. Links sensors to devices via MAC address
5. Only exposes metrics for sensors that have both device name and sensor name

## Usage

### Configuration Example
```yaml
logLevel: Information
listenPort: 8080
targets:
- name: my_device
  url: 192.168.0.10
  enableComponentMetrics: true
```

### Sample Metrics Output
```
shellyPro4Pm_my_device_component{device_name="Keller",sensor_name="Temperature"} 20.2
shellyPro4Pm_my_device_component{device_name="Keller",sensor_name="Humidity"} 73
shellyPro4Pm_my_device_component{device_name="Keller",sensor_name="Battery"} 100
shellyPro4Pm_my_device_component{device_name="Lüftung",sensor_name="Temperature"} 20.4
shellyPro4Pm_my_device_component{device_name="Lüftung",sensor_name="Humidity"} 63
shellyPro4Pm_my_device_component{device_name="Lüftung",sensor_name="Battery"} 100
```

## Extensibility

### Adding to Other Exporters

To add component support to other Shelly exporters:

1. **Add dependency**: Reference the `Utilities.Components` namespace
2. **Update TargetDevice**: Add `enableComponentMetrics` property
3. **Update Connection class**: 
   ```csharp
   readonly BtHomeComponentsHandler? componentsHandler;
   
   // In constructor:
   if (enableComponentMetrics)
   {
       string? password = target.RequiresAuthentication() ? target.password : null;
       componentsHandler = new BtHomeComponentsHandler(targetUrl, requestTimeoutTime, password);
   }
   
   // In UpdateMetricsIfNecessary, make simultaneous requests:
   Task<bool>? componentsTask = componentsHandler?.UpdateComponentsFromDevice();
   
   // Add methods:
   public string GetComponentMetrics(string metricPrefix) => 
       componentsHandler?.GenerateMetrics(metricPrefix) ?? "";
   ```
4. **Update Program.cs**:
   ```csharp
   // In CollectAllMetrics():
   if (device.HasComponentsEnabled())
   {
       allMetrics += device.GetComponentMetrics($"exporterName_{device.GetTargetName()}");
   }
   ```

### Component Request URL

All Shelly devices that support components use the same endpoint:
```
POST /rpc
{
  "method": "Shelly.GetComponents"
}
```

### Benefits

The simplified design provides:
- **Simplicity**: No unnecessary abstractions or interfaces (YAGNI principle)
- **Reliability**: No assumptions about sensor types based on user-provided names
- **Maintainability**: Straightforward, concrete implementation
- **Performance**: Direct method calls without interface overhead
- **Flexibility**: Users control sensor naming without system-imposed type constraints

## Error Handling

- Component request failures don't affect power meter metrics
- Invalid component data is logged but doesn't stop the exporter
- Missing device names or sensor names result in skipped metrics
- Graceful degradation when components are disabled

## Performance Considerations

- Simultaneous requests reduce total update time
- Components are cached and only updated when power metrics are updated
- Minimal memory overhead for component storage
- Thread-safe component data access

## Supported Sensor Types

The system uses user-provided sensor names directly in Prometheus labels without making assumptions about sensor types. This approach:
- Respects user naming preferences
- Avoids incorrect type classification 
- Provides maximum flexibility
- Ensures reliable metric generation regardless of naming conventions
