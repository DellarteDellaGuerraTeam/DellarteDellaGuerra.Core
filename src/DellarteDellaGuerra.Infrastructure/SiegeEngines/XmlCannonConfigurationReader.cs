using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using DellarteDellaGuerra.Domain.Common.Logging.Port;
using DellarteDellaGuerra.Domain.SiegeEngines.Port;
using DellarteDellaGuerra.Infrastructure.Utils;

namespace DellarteDellaGuerra.Infrastructure.SiegeEngines;

public class XmlCannonConfigurationReader : ICannonConfigurationReader
{
    private readonly ILogger _logger;

    public XmlCannonConfigurationReader(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<XmlCannonConfigurationReader>();
    }

    public IEnumerable<Domain.SiegeEngines.Model.Cannon> LoadCannons()
    {
        string? configPath = ResourceLocator.GetCannonXmlFilePath(_logger);

        if (configPath is null)
        {
            _logger.Error($"Could not find any xml for cannons.");
            return new List<Domain.SiegeEngines.Model.Cannon>();
        }

        var document = LoadAndValidateXml(configPath);
        var cannons = document.Descendants("Cannon")
            .Select(CreateCannons).ToList();

        foreach (var cannon in cannons) _logger.Debug($"Loaded '{cannon.Id}' cannon");

        return cannons;
    }

    private XDocument LoadAndValidateXml(string xmlPath)
    {
        try
        {
            var schemaPath = GetEmbeddedSchemaPath();
            if (schemaPath != null)
            {
                ValidateXmlAgainstSchema(xmlPath, schemaPath);
            }
            else
            {
                _logger.Warn("Cannon XML schema not found, skipping validation");
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"XML validation failed: {ex.Message}. Proceeding without schema validation.");
        }

        return XDocument.Load(xmlPath);
    }

    private string? GetEmbeddedSchemaPath()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "DellarteDellaGuerra.Infrastructure.SiegeEngines.cannons.xsd";
        
        if (assembly.GetManifestResourceNames().Contains(resourceName))
        {
            // Extract embedded resource to temporary file
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            
            var tempPath = Path.GetTempFileName();
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);
            return tempPath;
        }

        return null;
    }

    private void ValidateXmlAgainstSchema(string xmlPath, string schemaPath)
    {
        try
        {
            var settings = new XmlReaderSettings();
            settings.Schemas.Add("", schemaPath);
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationEventHandler += (sender, e) =>
            {
                var message = $"XML validation error: {e.Message} (Line: {e.Exception?.LineNumber}, Position: {e.Exception?.LinePosition})";
                if (e.Severity == XmlSeverityType.Error)
                {
                    throw new InvalidOperationException(message);
                }
                _logger.Warn(message);
            };

            using var reader = XmlReader.Create(xmlPath, settings);
            while (reader.Read()) { }
            
            _logger.Debug("Cannon XML validation successful");
        }
        finally
        {
            // Clean up temporary schema file
            if (File.Exists(schemaPath))
            {
                try
                {
                    File.Delete(schemaPath);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to delete temporary schema file: {ex.Message}");
                }
            }
        }
    }

    private static Domain.SiegeEngines.Model.Cannon CreateCannons(XElement element)
    {
        var id = element.Element("Id")?.Value ?? throw new InvalidOperationException("Cannon Id is required");
        var displayName = element.Element("DisplayName")?.Value ?? throw new InvalidOperationException($"DisplayName is required for cannon '{id}'");
        
        var machineTypeValue = element.Element("MachineType")?.Value;
        if (!int.TryParse(machineTypeValue, out var machineType))
        {
            throw new InvalidOperationException($"Invalid MachineType '{machineTypeValue}' for cannon '{id}'. Must be a valid integer.");
        }
        
        var boneIndexValue = element.Element("CampaignMapProjectileBoneIndex")?.Value;
        if (!int.TryParse(boneIndexValue, out var boneIndex))
        {
            throw new InvalidOperationException($"Invalid CampaignMapProjectileBoneIndex '{boneIndexValue}' for cannon '{id}'. Must be a valid integer.");
        }

        return new Domain.SiegeEngines.Model.Cannon(
            id,
            displayName,
            element.Element("SiegeDeploymentSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"SiegeDeploymentSelectionIconSpriteId is required for cannon '{id}'"),
            element.Element("MapSiegeMarkerSpriteId")?.Value ?? throw new InvalidOperationException($"MapSiegeMarkerSpriteId is required for cannon '{id}'"),
            element.Element("CampaignMapSelectionIconSpriteId")?.Value ?? throw new InvalidOperationException($"CampaignMapSelectionIconSpriteId is required for cannon '{id}'"),
            element.Element("CampaignMapPrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapPrefabName is required for cannon '{id}'"),
            element.Element("CampaignMapProjectilePrefabName")?.Value ?? throw new InvalidOperationException($"CampaignMapProjectilePrefabName is required for cannon '{id}'"),
            element.Element("CampaignMapReloadAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapReloadAnimationName is required for cannon '{id}'"),
            element.Element("CampaignMapFireAnimationName")?.Value ?? throw new InvalidOperationException($"CampaignMapFireAnimationName is required for cannon '{id}'"),
            machineType,
            boneIndex
        );
    }
}
