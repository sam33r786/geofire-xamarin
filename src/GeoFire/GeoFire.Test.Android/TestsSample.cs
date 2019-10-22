using System;
using System.Collections.Generic;
using Firebase.Firestore;
using GeoFire.Test.Android.Plugin.CloudFirestore;
using NUnit.Framework;
using Plugin.CloudFirestore;

namespace GeoFire.Test.Android
{
    [TestFixture]
    public class TestsSample
    {

        [SetUp]
        public void Setup() { }


        [TearDown]
        public void Tear() { }

        [Test]
        public void TestAddLocation()
        {
            var firestore = CrossCloudFirestore.Current.Instance;
            firestore.FirestoreSettings.AreTimestampsInSnapshotsEnabled = true;
            // var geoFire = new GeoFire(firestore, "test");
            try
            {
                firestore.Collection("test").Document("node1").Set(new Dictionary<string, string> {{"test", "wewe"}}.ToNativeFieldValues(), SetOptions.Merge());
                //await geoFire.SetLocationAsync("node1", new GeoPoint(0, 0));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            Assert.True(true);
        }

        [Test]
        public void Fail()
        {
            Assert.False(true);
        }

        [Test]
        [Ignore("another time")]
        public void Ignore()
        {
            Assert.True(false);
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive("Inconclusive");
        }
    }
}
