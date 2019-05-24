# Changelog

## 2.0.0
- Breaking: `ErrorReportingServiceName` and `ErrorReportingServiceVersion` config options changed to `ServiceName` and `ServiceVersion`.
- Service name and version metadata will be added to all log entries, and will allow exceptions at any level to be picked up by StackDriver error reporting.
- Improved log formatting and excepting handling, Better preformance and lower allocations.

## 1.10.0
- Fixed bug if there is no exception attached for "Error" entry.

## 1.9.0
- Added ability for log entries logged as "Error" to be sent to StackDriver error reporting.

## 1.7.0
- All options can now be configured through `appsettings.json`.
- New option to provide Google Application Credentials as JSON text.

## 1.6.0
- Improved handling of scalar values in JSON output by pattern matching on type instead of attempting to parse to double.
- WARNING: JSON output only accepts numeric values as `double` so all numbers will be converted. Large integers and floating-point values will lose precision. If you want the exact value preserved then send then log it as a string instead.

## 1.5.0
- Added support for .NET Core Configuration API, using [`serilog-settings-configuration`](https://github.com/serilog/serilog-settings-configuration)
- Labels can be provided in options object constructor or set using properties. Both will be merged together.
- Added [SourceLink](https://github.com/dotnet/sourcelink) support for source-code debugging.

## 1.4.7
- Added `ResourceLabels` configuration option to support additional labels for the Resource type.

## 1.4.6
- Fixed null value handling (logged as empty string) in text output.

## 1.4.5
- Additional data type handling for null, bool, numeric, and string scalar values in JSON/Protobuf output.

## 1.4.0
- Support fully structured logging in GCP via JSON/Protobuf output with configuration option.

## 1.3.1
- More efficient iteration of attached properties.

## 1.3.0
- Fixed property handling to handle all Serilog types: `ScalarValue`, `SequenceValue`, `StructureValue`, `DictionaryValue`.

## 1.2.0
- Option to disable automatic `SourceContext` property.
- Added TestWeb project to diagnose log output.

## 1.1.0
- Fixed property handling to support scalar and nested properties.

## 1.0.0
- Sink created for GCP stack driver.
