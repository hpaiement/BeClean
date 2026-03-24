using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeClean.Api.JsonConverters
{
    /// <summary>
    /// Class DateTimeNullableConverter.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DateTimeNullableConverter"/> class.
    /// </remarks>
    /// <param name="pattern">The pattern.</param>
    public class DateTimeNullableConverter(string pattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss") : JsonConverter<DateTime?>
    {
        /// <summary>
        /// Reads and converts the JSON to type
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TokenType != JsonTokenType.Null)
                return DateTime.Parse(reader.GetString()!);
            else
                return null;
        }

        /// <summary>
        /// Writes a specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value != null)
                writer.WriteStringValue(((DateTime)value).ToString(pattern));
            else
                writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Class DateTimeConverter.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DateTimeConverter"/> class.
    /// </remarks>
    /// <param name="pattern">The pattern.</param>
    public class DateTimeConverter(string pattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss") : JsonConverter<DateTime>
    {

        /// <summary>
        /// Reads and converts the JSON to type
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="NullReferenceException">This is a non-nullable field</exception>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Null)
                return DateTime.Parse(reader.GetString()!);
            else
                throw new NullReferenceException("This is a non-nullable field");
        }

        /// <summary>
        /// Writes a specified value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(pattern));
        }
    }
}
