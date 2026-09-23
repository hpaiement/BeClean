using System.Linq.Expressions;
using System.Reflection;

namespace BeClean.DataLayer.Repositories.Bulk
{
    internal static class PropertyExpression
    {
        /// <summary>
        /// Returns the properties selected by a strong typed property expression. Supported formats:
        /// x => x.Col1, x => (object)x.Col1 and x => new { x.Col1, x.Col2 }
        /// </summary>
        /// <param name="propertyExpression"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IEnumerable<PropertyInfo> GetProperties<TEntity>(Expression<Func<TEntity, object>>? propertyExpression)
        {
            if (propertyExpression == null)
                return Enumerable.Empty<PropertyInfo>();

            IEnumerable<PropertyInfo> properties;
            if (propertyExpression.Body is NewExpression newExpression && newExpression.Members != null)
            {
                // Anonymous object with multiple properties
                properties = newExpression.Arguments.Select(a => (MemberExpression)a).Select(m => (PropertyInfo)m.Member).ToList();
            }
            else if (propertyExpression.Body is MemberExpression memberExpression)
            {
                // Single property (e.g., x => x.Col1)
                properties = new List<PropertyInfo> { (PropertyInfo)memberExpression.Member };
            }
            else if (
                propertyExpression.Body is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Operand is MemberExpression unaryMemberExpression)
            {
                // Handles boxing for object (e.g., x => (object)x.PrimaryId)
                properties = new List<PropertyInfo> { (PropertyInfo)unaryMemberExpression.Member };
            }
            else
                throw new Exception("Property expression format not supported (expected x => x.Col or x => new { x.Col1, x.Col2 })");

            return properties;
        }
    }
}
