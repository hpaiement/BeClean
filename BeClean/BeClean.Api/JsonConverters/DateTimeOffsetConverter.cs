using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeClean.Api.JsonConverters
{
    public class DateTimeOffsetConverter(string pattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss") : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Null)
                return DateTimeOffset.Parse(reader.GetString()!);
            else
                throw new NullReferenceException("This is a non-nullable field");
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(pattern));
        }
    }

    public class DateTimeOffsetNullableConverter(string pattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss") : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Null)
                return DateTimeOffset.Parse(reader.GetString()!);
            else
                return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if(value != null)
                writer.WriteStringValue(((DateTimeOffset)value).ToString(pattern));
            else
                writer.WriteNullValue();
        }
    }
}
