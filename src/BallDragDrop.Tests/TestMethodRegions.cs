namespace BallDragDrop.Services
{
    public class TestClass
    {
        // This method should trigger BDD4001 - Method not in region
        public void MethodWithoutRegion()
        {
            // Method implementation
        }

        #region CorrectMethod
        // This method should not trigger any diagnostics
        public void CorrectMethod()
        {
            // Method implementation
        }
        #endregion

        #region WrongRegionName
        // This method should trigger BDD4002 - Incorrect region naming
        public void MethodWithWrongRegion()
        {
            // Method implementation
        }
        #endregion

        // Property should not trigger diagnostics
        public string TestProperty { get; set; }

        // Property with explicit accessors should not trigger diagnostics
        private string _field;
        public string ExplicitProperty
        {
            get { return _field; }
            set { _field = value; }
        }
    }
}