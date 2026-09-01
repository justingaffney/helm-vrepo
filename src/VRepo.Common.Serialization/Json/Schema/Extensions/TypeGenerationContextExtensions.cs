using System.Reflection;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace VRepo.Common.Serialization.Json.Schema.Extensions;

public static class TypeGenerationContextExtensions
{
    public static TypeGenerationContext Clone(this TypeGenerationContext context)
    {
        // New instances cannot be created normally because the constructor is internal, use reflection to invoke it
        var constructor = typeof(TypeGenerationContext).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Type)],
            null);
        var clonedContext = (TypeGenerationContext)constructor!.Invoke([context.Type]);

        // Regenerate the intents, again using reflection as the method is also internal
        var generateIntentsMethod = typeof(TypeGenerationContext).GetMethod(
            "GenerateIntents",
            BindingFlags.NonPublic | BindingFlags.Instance,
            []);
        generateIntentsMethod!.Invoke(clonedContext, []);

        return clonedContext;
    }

    public static void AddNullSchemaValueType(this TypeGenerationContext context)
    {
        var typeIntent = context.Intents.OfType<TypeIntent>().FirstOrDefault();
        if (typeIntent != null)
        {
            context.Intents.Remove(typeIntent);
            context.Intents.Add(new TypeIntent(typeIntent.Type | SchemaValueType.Null));
        }
        else
        {
            context.Intents.Add(new TypeIntent(SchemaValueType.Null));
        }
    }
}
