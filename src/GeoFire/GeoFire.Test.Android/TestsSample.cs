using System;
using System.Threading;
using System.Threading.Tasks;
using Plugin.CloudFirestore;
using Xunit;

namespace GeoFire.Test.Android
{
    public class TestsSample
    {

        //[SetUp]
        //public void Setup() { }


        //[TearDown]
        //public void Tear() { }

        [Fact]
        public async Task GetAndSetLocation()
        {
            var geoFire = new GeoFire("test");
            try
            {
                await geoFire.SetLocationAsync("ququ2", new GeoPoint(5, 10));
                var geoPoint = await geoFire.GetLocationAsync("ququ2");
                Assert.Equal(new GeoPoint(5, 10), geoPoint);
            }
            catch (Exception e)
            {
                Assert.Null(e);
            }
            Assert.True(true);
        }
    }
}
