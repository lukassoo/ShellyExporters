using System.Text.Json;
using Utilities.Components;
using Xunit;

namespace Utilities.Tests.Components;

public class BtHomeComponentsHandlerTests
{
    const string SampleComponentsResponse = """
{
  "result": {
    "components": [
      {
        "key": "bthomedevice:200",
        "status": {
          "id": 200,
          "rssi": -49,
          "battery": 100,
          "last_updated_ts": 1753167523
        },
        "config": {
          "id": 200,
          "addr": "aa:bb:cc:dd:ee:f1",
          "name": "Lüftung"
        }
      },
      {
        "key": "bthomedevice:201",
        "status": {
          "id": 201,
          "rssi": -69,
          "battery": 100,
          "last_updated_ts": 1753167527
        },
        "config": {
          "id": 201,
          "addr": "aa:bb:cc:dd:ee:f2",
          "name": "Keller"
        }
      },
      {
        "key": "bthomesensor:200",
        "status": {
          "id": 200,
          "value": 100,
          "last_updated_ts": 1753167523
        },
        "config": {
          "id": 200,
          "addr": "aa:bb:cc:dd:ee:f1",
          "name": "Battery",
          "obj_id": 1,
          "idx": 0
        }
      },
      {
        "key": "bthomesensor:201",
        "status": {
          "id": 201,
          "value": 63,
          "last_updated_ts": 1753167523
        },
        "config": {
          "id": 201,
          "addr": "aa:bb:cc:dd:ee:f1",
          "name": "Humidity",
          "obj_id": 46,
          "idx": 0
        }
      },
      {
        "key": "bthomesensor:202",
        "status": {
          "id": 202,
          "value": 20.4,
          "last_updated_ts": 1753167523
        },
        "config": {
          "id": 202,
          "addr": "aa:bb:cc:dd:ee:f1",
          "name": "Temperature",
          "obj_id": 69,
          "idx": 0
        }
      },
      {
        "key": "bthomesensor:203",
        "status": {
          "id": 203,
          "value": 100,
          "last_updated_ts": 1753167527
        },
        "config": {
          "id": 203,
          "addr": "aa:bb:cc:dd:ee:f2",
          "name": "Battery",
          "obj_id": 1,
          "idx": 0
        }
      },
      {
        "key": "bthomesensor:204",
        "status": {
          "id": 204,
          "value": 73,
          "last_updated_ts": 1753167527
        },
        "config": {
          "id": 204,
          "addr": "aa:bb:cc:dd:ee:f2",
          "name": "Humidity",
          "obj_id": 46,
          "idx": 0
        }
      },
      {
        "key": "bthomesensor:205",
        "status": {
          "id": 205,
          "value": 20.2,
          "last_updated_ts": 1753167527
        },
        "config": {
          "id": 205,
          "addr": "aa:bb:cc:dd:ee:f2",
          "name": "Temperature",
          "obj_id": 69,
          "idx": 0
        }
      }
    ]
  }
}
""";

    [Fact]
    public void UpdateComponents_ShouldParseDevicesAndSensorsCorrectly()
    {
        // Arrange
        var handler = new BtHomeComponentsHandler();

        // Act
        bool result = handler.UpdateComponents(SampleComponentsResponse);

        // Assert
        Assert.True(result);
        
        var devices = handler.GetDevices();
        Assert.Equal(2, devices.Count);

        var lueftungDevice = devices.FirstOrDefault(d => d.Name == "Lüftung");
        Assert.NotNull(lueftungDevice);
        Assert.Equal("aa:bb:cc:dd:ee:f1", lueftungDevice.Address);
        Assert.Equal(3, lueftungDevice.Sensors.Count);

        var kellerDevice = devices.FirstOrDefault(d => d.Name == "Keller");
        Assert.NotNull(kellerDevice);
        Assert.Equal("aa:bb:cc:dd:ee:f2", kellerDevice.Address);
        Assert.Equal(3, kellerDevice.Sensors.Count);
    }

    [Fact]
    public void GenerateMetrics_ShouldCreatePrometheusFormattedOutput()
    {
        // Arrange
        var handler = new BtHomeComponentsHandler();
        handler.UpdateComponents(SampleComponentsResponse);

        // Act
        string metrics = handler.GenerateMetrics("test_device");

        // Assert
        Assert.Contains("test_device_component{device_name=\"Lüftung\",sensor_name=\"Battery\"} 100", metrics);
        Assert.Contains("test_device_component{device_name=\"Lüftung\",sensor_name=\"Humidity\"} 63", metrics);
        Assert.Contains("test_device_component{device_name=\"Lüftung\",sensor_name=\"Temperature\"} 20.4", metrics);
        Assert.Contains("test_device_component{device_name=\"Keller\",sensor_name=\"Battery\"} 100", metrics);
        Assert.Contains("test_device_component{device_name=\"Keller\",sensor_name=\"Humidity\"} 73", metrics);
        Assert.Contains("test_device_component{device_name=\"Keller\",sensor_name=\"Temperature\"} 20.2", metrics);
    }

    [Fact]
    public void BtHomeSensor_ShouldGenerateCorrectLabels()
    {
        // Arrange
        var sensor = new BtHomeSensor(200, "Temperature", 20.5, 1753167523, 69, 0);

        // Act
        string labels = sensor.GetLabels("TestDevice");

        // Assert
        Assert.Equal("{device_name=\"TestDevice\",sensor_name=\"Temperature\"}", labels);
    }

    [Fact]
    public void BtHomeDevice_ShouldManageSensorsCorrectly()
    {
        // Arrange
        var device = new BtHomeDevice(200, "TestDevice", "aa:bb:cc:dd:ee:ff");
        var sensor1 = new BtHomeSensor(1, "Battery", 100, 0, 1, 0);
        var sensor2 = new BtHomeSensor(2, "Temperature", 22.5, 0, 69, 0);

        // Act
        device.AddSensor(sensor1);
        device.AddSensor(sensor2);

        // Assert
        Assert.Equal(2, device.Sensors.Count);
        Assert.Contains(sensor1, device.GetSensors());
        Assert.Contains(sensor2, device.GetSensors());
    }
}
