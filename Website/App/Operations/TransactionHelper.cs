namespace Website.App.Operations
{
    public class TransactionHelper
    {
        public enum OriginType { Vendor, Customer, Friend, Unknown }
        public enum RelationshipType { PrivateFriend, Commercial, Vendor, Unknown }
        public enum TransactionNature { NonTaxable, Taxable }

        public record TransactionContext(OriginType Origin, RelationshipType Relationship, TransactionNature Nature, bool IncludeInGSTReturn, bool AffectsIncomeExpense, string AccountingProfile);
        public static class TransactionResolver
        {
            public static TransactionContext Resolve(OriginType origin, bool isFriendOfOrigin, bool driverIsFriendsOnly, bool driverIsCommercial)
            {
                if (origin == OriginType.Vendor)
                    return new TransactionContext(origin, RelationshipType.Vendor, TransactionNature.Taxable, true, true, "COMMERCIAL_STANDARD");

                if (origin == OriginType.Friend)
                    return new TransactionContext(origin, RelationshipType.PrivateFriend, TransactionNature.NonTaxable, false, false, "PRIVATE_CLEARING");

                if (origin == OriginType.Customer)
                {
                    if (isFriendOfOrigin)
                        return new TransactionContext(origin, RelationshipType.PrivateFriend, TransactionNature.NonTaxable, false, false, "PRIVATE_CLEARING");

                    return new TransactionContext(origin, RelationshipType.Commercial, TransactionNature.Taxable, true, true, "COMMERCIAL_STANDARD");
                }

                return new TransactionContext(OriginType.Unknown, RelationshipType.Unknown, TransactionNature.NonTaxable, false, false, "PRIVATE_MISC");
            }
        }
    }
}
