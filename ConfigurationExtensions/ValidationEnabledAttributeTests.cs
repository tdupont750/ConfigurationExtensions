using Xunit;

namespace ConfigurationExtensions
{
    public class ValidationEnabledAttributeTests
    {
        [Fact]
        public void HasAttribute()
        {
            var o = new HasAttributeClass();
            var isEnabled1 = ValidationEnabledAttribute.IsEnabled(o);
            Assert.False(isEnabled1);

            o.IsEnabled = true;
            var isEnabled2 = ValidationEnabledAttribute.IsEnabled(o);
            Assert.True(isEnabled2);
        }

        [Fact]
        public void NoAttribute()
        {
            var o = new NoAttributeClass();
            var isEnabled1 = ValidationEnabledAttribute.IsEnabled(o);
            Assert.True(isEnabled1);

            o.IsEnabled = true;
            var isEnabled2 = ValidationEnabledAttribute.IsEnabled(o);
            Assert.True(isEnabled2);
        }

        internal class HasAttributeClass
        {
            [ValidationEnabled]
            public bool IsEnabled { get; set; }
        }

        internal class NoAttributeClass
        {
            public bool IsEnabled { get; set; }
        }
    }
}
