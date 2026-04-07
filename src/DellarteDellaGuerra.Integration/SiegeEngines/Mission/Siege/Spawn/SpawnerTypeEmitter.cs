using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Bannerlord.Cannons.BattleMechanics.Artillery;
using TaleWorlds.Engine;

namespace DellarteDellaGuerra.Integration.SiegeEngines.Mission.Siege.Spawn;

public static class SpawnerTypeEmitter
{
    private static readonly ModuleBuilder _module = AssemblyBuilder
        .DefineDynamicAssembly(new AssemblyName("DadgDynamicSpawners"), AssemblyBuilderAccess.Run)
        .DefineDynamicModule("DadgDynamicSpawnersModule");
    private static Type? _emittedSpawnerType;

    public static Type EmitSpawnerType()
    {
        if (_emittedSpawnerType != null) return _emittedSpawnerType;

        var typeBuilder = _module.DefineType(
            "GenericCannonSpawner",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(GenericCannonSpawnerBase)
        );

        var editorVisibleCtor = typeof(EditorVisibleScriptComponentVariable)
            .GetConstructor(new[] { typeof(bool) })!;

        var artilleryFields = typeof(ArtilleryRangedSiegeWeapon)
            .GetFields(BindingFlags.Public | BindingFlags.Instance);
        var usedFieldHashes = CollectManagedEditableFieldHashes(typeof(GenericCannonSpawnerBase));

        // Build fields and keep FieldBuilder references for the constructor emitter
        var fieldBuilders = new Dictionary<string, FieldBuilder>(artilleryFields.Length);
        foreach (var field in artilleryFields)
        {
            if (!IsManagedSupportedEditableFieldType(field.FieldType)) continue;

            var fieldHash = GetManagedHash(field.Name);
            if (!usedFieldHashes.Add(fieldHash)) continue;

            var fb = typeBuilder.DefineField(field.Name, field.FieldType, FieldAttributes.Public);
            fb.SetCustomAttribute(new CustomAttributeBuilder(editorVisibleCtor, new object[] { true }));
            fieldBuilders[field.Name] = fb;
        }

        EmitConstructorWithDefaults(typeBuilder, artilleryFields, fieldBuilders, _artilleryFieldDefaults);

        _emittedSpawnerType = typeBuilder.CreateTypeInfo()!.AsType();
        return _emittedSpawnerType;
    }

    private static HashSet<uint> CollectManagedEditableFieldHashes(Type type)
    {
        var hashes = new HashSet<uint>();
        while (type != null)
        {
            var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var editableAttr = field.GetCustomAttributes(typeof(EditorVisibleScriptComponentVariable), true)
                    .FirstOrDefault() as EditorVisibleScriptComponentVariable;
                var visible = editableAttr?.Visible ?? (!field.IsPrivate && !field.IsFamily);
                if (!visible) continue;
                hashes.Add(GetManagedHash(field.Name));
            }
            type = type.BaseType;
        }

        return hashes;
    }

    private static bool IsManagedSupportedEditableFieldType(Type fieldType)
    {
        if (fieldType.IsEnum) return true;
        return fieldType == typeof(int)
               || fieldType == typeof(float)
               || fieldType == typeof(bool)
               || fieldType == typeof(string);
    }

    private static uint GetManagedHash(string text)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        uint hash = 5381;
        foreach (var b in bytes) hash = ((hash << 5) + hash) + b;
        return hash;
    }

    private static void EmitConstructorWithDefaults(
        TypeBuilder typeBuilder,
        FieldInfo[] artilleryFields,
        Dictionary<string, FieldBuilder> fieldBuilders,
        Dictionary<string, object?> defaults)
    {
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = ctorBuilder.GetILGenerator();

        // Chain to base()
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(GenericCannonSpawnerBase).GetConstructor(Type.EmptyTypes)!);

        foreach (var field in artilleryFields)
        {
            if (!defaults.TryGetValue(field.Name, out var value) || value == null) continue;
            if (!fieldBuilders.TryGetValue(field.Name, out var fb)) continue;

            il.Emit(OpCodes.Ldarg_0);

            if (field.FieldType == typeof(float))
                il.Emit(OpCodes.Ldc_R4, (float)value);
            else if (field.FieldType == typeof(int))
                il.Emit(OpCodes.Ldc_I4, (int)value);
            else if (field.FieldType == typeof(bool))
                il.Emit((bool)value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            else if (field.FieldType == typeof(string))
            {
                var s = (string)value;
                if (string.IsNullOrEmpty(s))
                {
                    il.Emit(OpCodes.Pop);
                    continue;
                }
                il.Emit(OpCodes.Ldstr, s);
            }
            else
            {
                il.Emit(OpCodes.Pop);
                continue;
            }

            il.Emit(OpCodes.Stfld, fb);
        }

        il.Emit(OpCodes.Ret);
    }

    // Hardcoded defaults mirroring field initializers across ArtilleryRangedSiegeWeapon and its
    // base classes (BaseFieldSiegeWeapon, RangedSiegeWeapon). Only non-zero / non-null / non-empty
    // values are listed — CLR defaults (0, false, null, "") are already correct.
    // Do NOT use Activator.CreateInstance(typeof(GenericCannon)) here: ScriptComponentBehavior
    // constructors register a native managed object without an engine context, leaving a dangling
    // object ID in Bannerlord's table. That orphaned ID later causes a null-dereference inside
    // Managed_GetStringArrayLength when the game tries to read string parameters.
    private static readonly Dictionary<string, object?> _artilleryFieldDefaults = new()
    {
        // ArtilleryRangedSiegeWeapon
        { nameof(ArtilleryRangedSiegeWeapon.FireSoundID),           "mortar_shot_1" },
        { nameof(ArtilleryRangedSiegeWeapon.FireSoundID2),          "mortar_shot_2" },
        { nameof(ArtilleryRangedSiegeWeapon.RecoilDuration),        0.8f },
        { nameof(ArtilleryRangedSiegeWeapon.Recoil2Duration),       0.8f },
        { nameof(ArtilleryRangedSiegeWeapon.DisplayName),           "Artillery" },
        { nameof(ArtilleryRangedSiegeWeapon.BaseMuzzleVelocity),    40f },
        // { nameof(ArtilleryRangedSiegeWeapon.RecoilDistance),        0.6f },
        { nameof(ArtilleryRangedSiegeWeapon.SlideBackFrameFactor),  0.6f },
        // { nameof(ArtilleryRangedSiegeWeapon.WheelRadius),           0.3f },
        { nameof(ArtilleryRangedSiegeWeapon.WheelRotationAxis),     "X" },
        // { nameof(ArtilleryRangedSiegeWeapon.PushStandingPointTag),  "push_cannon" },
        // RangedSiegeWeapon (inherited)
        { "startingAmmoCount",               20 },
        { "TopReleaseAngleRestriction",      1.5707964f },
        { "BottomReleaseAngleRestriction",   -1.5707964f },
        { "PilotStandingPointTag",           "Pilot" },
        { "AmmoPickUpTag",                   "ammopickup" },
        { "WaitStandingPointTag",            "Wait" },
    };
}
