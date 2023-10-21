using Dalamud.Interface.Windowing;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using System;
using System.Numerics;
using System.IO;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System.Reflection;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.Havok;
using Lumina.Text;
using Lumina.Data;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using System.Formats.Asn1;
using System.Text;

namespace MultilingualLuminaHelper.Windows;

public class Main : Window, IDisposable
{
    // 选择的语言
    private Dalamud.ClientLanguage selectedLang;
    // 选择的表格
    private Type selectedSheet = null!;
    // 获取到的类(表格)数量
    private int currentSheetsCount = 0;
    // 获取到的表格
    private Type[] sheetNames = null!;
    // 表格搜索过滤器
    private string searchFilter = string.Empty;

    private Plugin Plugin;

    public Main(Plugin plugin) : base(
        "Multilingual Lumina Helper", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;

        Init();
    }

    private void Init()
    {
        currentSheetsCount = GetClassCounts();
        
    }

    public override void Draw()
    {
        ShowSheetsCount();

        LangSelect();
        ImGui.SameLine();
        SheetSelect();

        if (ImGui.Button("生成CSV"))
        {
            var filePath = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, $"{selectedSheet}_{selectedLang}.csv");
            GenerateCsv(filePath);
        }


    }

    private void ShowSheetsCount()
    {
        if (currentSheetsCount != 0)
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, "表格数量:");
            ImGui.SameLine();
            ImGui.Text(currentSheetsCount.ToString());
            ImGui.SameLine();
            if (ImGui.Button("重新获取"))
            {
                currentSheetsCount = GetClassCounts();
            }
        }
    }

    private int GetClassCounts(string assemblyPath = "-1")
    {
        var targetNamespace = "Lumina.Excel.GeneratedSheets";

        if (assemblyPath == "-1")
        {
            assemblyPath = Path.Combine(Plugin.Instance.PluginInterface.ConfigDirectory.FullName, "Lumina.Excel.dll");
        }

        var targetAssembly = Assembly.LoadFrom(assemblyPath);

        sheetNames = targetAssembly.GetTypes()
            .Where(type => type.Namespace == targetNamespace)
            .ToArray();

        var classCount = sheetNames.Length;

        Service.PluginLog.Debug($"命名空间 {targetNamespace} 下的类数量: {classCount}");

        return classCount;
    }

    private void LangSelect()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudYellow, "语言:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(130);
        if (ImGui.BeginCombo("##LangSelect", selectedLang.ToString()))
        {
            foreach (Dalamud.ClientLanguage enumValue in Enum.GetValues(typeof(Dalamud.ClientLanguage)))
            {
                var isSelected = enumValue == selectedLang;
                if (ImGui.Selectable(enumValue.ToString(), isSelected))
                {
                    selectedLang = enumValue;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
        Service.DataManager.GetExcelSheet<TerritoryType>(Dalamud.ClientLanguage.German);
    }

    private void SheetSelect()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudYellow, "表格名:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(270);

        ImGui.SetNextWindowSize(new Vector2(280, 300));
        if (ImGui.BeginCombo("##SheetSelect", selectedSheet?.Name ?? "未选择"))
        {
            ImGui.InputText("##SheetSearch", ref searchFilter, 100);
            ImGui.Separator();
            foreach (var sheetName in sheetNames)
            {
                if (string.IsNullOrEmpty(searchFilter) || sheetName.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    var isSelected = sheetName == selectedSheet;
                    if (ImGui.Selectable(sheetName.Name, isSelected))
                    {
                        selectedSheet = sheetName;
                        GetSheet(selectedSheet);
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }

            ImGui.EndCombo();
        }
        Service.DataManager.GetExcelSheet<TerritoryType>(Dalamud.ClientLanguage.German);
    }

    private void GetSheet(Type selectedSheet)
    {
        PropertyInfo[] properties = selectedSheet.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            Type propertyType = property.PropertyType;
            if (propertyType == typeof(SeString) || propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(LazyRow<>))
            {
                Service.PluginLog.Debug($"Property Name: {property.Name}, Type: {propertyType}");
            }
        }
    }

    public void GenerateCsv(string filePath)
    {
        PropertyInfo[] properties = selectedSheet.GetProperties();

        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            writer.Write("RowId,");
            writer.WriteLine(string.Join(",", properties.Select(p => p.Name)));

            Type[] paramTypes = new Type[] { selectedLang.GetType() };

            MethodInfo getExcelSheetMethod = typeof(Dalamud.Plugin.Services.IDataManager).GetMethod("GetExcelSheet", paramTypes)
                .MakeGenericMethod(selectedSheet);

            var data = getExcelSheetMethod.Invoke(Service.DataManager, new object[] { selectedLang });


            foreach (var item in (System.Collections.IEnumerable)data)
            {
                var rowId = selectedSheet.GetProperty("RowId").GetValue(item);

                writer.Write(rowId);

                foreach (var property in properties)
                {
                    var value = property.GetValue(item);

                    if (property.PropertyType == typeof(SeString))
                    {
                        var rawString = property.PropertyType.GetProperty("RawString").GetValue(value);
                        writer.Write($",{rawString}");
                    }
                    else if (property.PropertyType.IsGenericType &&
                             property.PropertyType.GetGenericTypeDefinition() == typeof(LazyRow<>))
                    {
                        var lazyValue = property.PropertyType.GetProperty("Value").GetValue(value);
                        var firstProperty = lazyValue.GetType().GetProperties().First();
                        var firstPropertyValue = firstProperty.GetValue(lazyValue);
                        writer.Write($",{firstPropertyValue}");
                    }
                    else
                    {
                        writer.Write($",{value}");
                    }
                }

                writer.WriteLine();
            }
        }
    }

    public void Dispose()
    {
    }
}
