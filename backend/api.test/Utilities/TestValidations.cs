using System;
using Api.Utilities;
using Xunit;

namespace Api.Test.Utilities
{
    public class TestValidations
    {
        [Theory]
        [InlineData("123e4567-e89b-12d3-a456-426614174000")] // Valid UUID
        [InlineData("{123e4567-e89b-12d3-a456-426614174000}")] // Valid UUID with braces
        [InlineData("(123e4567-e89b-12d3-a456-426614174000)")] // Valid UUID with parentheses
        public void UUID_ValidInput_ReturnsSanitizedUUID(string validUUID)
        {
            var result = Validate.UUID(validUUID);
            Assert.Equal(validUUID.Replace("\n", "").Replace("\r", ""), result);
        }

        [Theory]
        [InlineData("invalid-uuid")] // Invalid UUID
        [InlineData("123e4567-e89b-12d3-a456-42661417400")] // Missing character
        [InlineData("")] // Empty string
        public void UUID_InvalidInput_ThrowsArgumentException(string invalidUUID)
        {
            Assert.Throws<ArgumentException>(() => Validate.UUID(invalidUUID));
        }
    }
}
