using System.Collections.Generic;
using Oumijia.Core.Classification;
using Oumijia.Core.Edz;
using Xunit;

namespace Oumijia.Core.Tests;

public class ClassifierTests
{
    private static Classifier NewClassifier() => new(ClassifierRules.CreateDefault());

    private static EdzPackageInfo PartInfo(string manufacturer, string partNumber, string productGroup)
    {
        var info = new EdzPackageInfo();
        info.Parts.Add(new EdzPartInfo
        {
            Manufacturer = manufacturer,
            PartNumber = partNumber,
            ProductGroup = productGroup,
            Designation = productGroup
        });
        return info;
    }

    [Fact]
    public void Classifies_mcb_from_product_group()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("ABB", "S201-C16", "Miniature circuit breaker"));

        Assert.Equal("ABB", result.ManufacturerFolder);
        Assert.Equal("MCB", result.CategoryFolder);
    }

    [Fact]
    public void Classifies_plc_before_other_matches()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("Siemens", "6ES7214-1AG40", "SIMATIC S7-1200 CPU 1214C"));

        Assert.Equal("Siemens", result.ManufacturerFolder);
        Assert.Equal("PLC", result.CategoryFolder);
    }

    [Fact]
    public void Classifies_psu_from_designation()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("Phoenix Contact", "QUINT-PS-100", "Power supply 24V DC"));

        Assert.Equal("Phoenix Contact", result.ManufacturerFolder);
        Assert.Equal("PSU", result.CategoryFolder);
    }

    [Fact]
    public void Normalizes_manufacturer_suffix()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("Siemens AG", "3RT2024", "Contactor"));

        Assert.Equal("Siemens", result.ManufacturerFolder);
    }

    [Fact]
    public void Applies_manufacturer_alias_dictionary()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("ABB Ltd.", "S201", "Miniature circuit breaker"));

        Assert.Equal("ABB", result.ManufacturerFolder);
    }

    [Fact]
    public void Falls_back_when_nothing_is_known()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("mystery.edz", null);

        Assert.Equal("_Unknown", result.ManufacturerFolder);
        Assert.Equal("_Unsorted", result.CategoryFolder);
        Assert.Equal(Confidence.Low, result.CategoryConfidence);
    }

    [Fact]
    public void Falls_back_to_unsorted_for_unknown_product_group()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("ACME", "WIDGET-1", "Miscellaneous gadget"));

        Assert.Equal("ACME", result.ManufacturerFolder);
        Assert.Equal("_Unsorted", result.CategoryFolder);
    }

    [Fact]
    public void Suggests_relative_folder_path()
    {
        var classifier = NewClassifier();
        var result = classifier.Classify("x.edz", PartInfo("Schneider Electric", "LC1D18", "Contactor"));

        Assert.Equal("Schneider Electric/Contactor", result.SuggestedRelativeFolder());
    }

    [Fact]
    public void Matches_custom_eclass_prefix_rule()
    {
        var rules = ClassifierRules.CreateDefault();
        rules.Categories.Insert(0, new CategoryRule
        {
            Folder = "SpecialGroup",
            EClassPrefixes = new List<string> { "27-14" }
        });

        var classifier = new Classifier(rules);
        var info = PartInfo("ACME", "X1", "Nothing matching by keyword");
        info.Parts[0].EClassCode = "27-14-05-11";

        var result = classifier.Classify("x.edz", info);
        Assert.Equal("SpecialGroup", result.CategoryFolder);
    }
}
