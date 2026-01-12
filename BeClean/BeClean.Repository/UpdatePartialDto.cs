using System.Text.Json;

namespace BeClean.Repository
{
    /// <summary>
    /// Generic dto to use for implementing partial update in controller. With this dto, it is possible to update a model with a user selected key and
    /// a dynamic set of values to update.
    /// </summary>
    public class UpdatePartialDto
    {
        /// <summary>
        /// Dictonary of key field(s) to identify which entity(ies) to update with provided field Values
        /// </summary>
        public Dictionary<string, JsonElement> Key { get; set; } = [];
        /// <summary>
        /// Field(s) to update with given value
        /// </summary>
        public Dictionary<string, JsonElement> Values { get; set; } = [];
    }
}

