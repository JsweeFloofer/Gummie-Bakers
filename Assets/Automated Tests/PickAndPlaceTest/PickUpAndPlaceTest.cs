using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Unity.Netcode;

public class PickUpAndPlaceTest : InputTestFixture
{
    private Keyboard keyboard;
    private GameObject testRoot;

    public override void Setup()
    {
        base.Setup();

        keyboard = InputSystemEventSystem.AddDevice<Keyboard>();
    }
    
    
    [UnityTest]
    public IEnumerator PlayerPicksUpItemAndDeliversIt()
    {
        testRoot = new GameObject("ItemDeliveryTestRoot");

        try
        {
            // ARRANGE: create a predictable test environment.
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(testRoot.transform);
            playerObject.transform.position = Vector3.zero;

            var body = playerObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            playerObject.AddComponent<CapsuleCollider>();

            var mover = playerObject.AddComponent<PlayerMover>();

            var carryAnchorObject = new GameObject("CarryAnchor");
            carryAnchorObject.transform.SetParent(
                playerObject.transform);
            carryAnchorObject.transform.localPosition =
                new Vector3(0f, 1.2f, 0.5f);

            var interactor =
                playerObject.AddComponent<PlayerInteractor>();

            // Assign the private serialized reference for this example.
            var anchorField = typeof(PlayerInteractor).GetField(
                "carryAnchor",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            anchorField.SetValue(
                interactor, carryAnchorObject.transform);

            // Item starts three units in front of the player.
            var itemObject = new GameObject("Item");
            itemObject.transform.SetParent(testRoot.transform);
            itemObject.transform.position =
                new Vector3(0f, 0f, 3f);

            itemObject.AddComponent<BoxCollider>();
            itemObject.AddComponent<Rigidbody>();
            var item = itemObject.AddComponent<CarryItem>();

            // Destination is farther along the same path.
            var destinationObject =
                new GameObject("Destination");

            destinationObject.transform.SetParent(
                testRoot.transform);
            destinationObject.transform.position =
                new Vector3(0f, 0f, 8f);

            var zoneCollider =
                destinationObject.AddComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.size = new Vector3(3f, 2f, 2f);

            destinationObject.AddComponent<DeliveryZone>();

            yield return null;

            Assert.IsNull(interactor.HeldItem);
            Assert.IsFalse(item.IsDelivered);

            // ACT 1: move toward the item using actual input.
            Press(keyboard.upArrowKey);

            yield return WaitForCondition(
                () => Vector3.Distance(
                    playerObject.transform.position,
                    itemObject.transform.position) <= 1.0f,
                5f,
                "Player never reached the item.");

            Release(keyboard.upArrowKey);
            yield return new WaitForFixedUpdate();

            // ACT 2: press the pickup button.
            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);

            yield return WaitForCondition(
                () => interactor.HeldItem == item,
                2f,
                "Player did not pick up the item.");

            // ASSERT: item is being carried.
            Assert.AreSame(item, interactor.HeldItem);
            Assert.AreSame(
                carryAnchorObject.transform,
                item.transform.parent);

            // ACT 3: carry the item to the destination.
            Press(keyboard.upArrowKey);

            yield return WaitForCondition(
                () => item.IsDelivered,
                8f,
                "Item was not delivered before timeout.");

            Release(keyboard.upArrowKey);

            // ASSERT: delivery succeeded.
            Assert.IsTrue(item.IsDelivered);
            Assert.IsNull(interactor.HeldItem);
            Assert.IsFalse(itemObject.activeSelf);
        }
        finally
        {
            ReleaseAllKeys();

            if (testRoot != null)
                UnityEngine.Object.Destroy(testRoot);
        }

        yield return null;
    }

    private IEnumerator WaitForCondition(
        Func<bool> condition,
        float timeoutSeconds,
        string failureMessage)
    {
        float deadline =
            Time.realtimeSinceStartup + timeoutSeconds;

        while (!condition())
        {
            if (Time.realtimeSinceStartup >= deadline)
                Assert.Fail(failureMessage);

            yield return null;
        }
    }

    private void ReleaseAllKeys()
    {
        if (keyboard == null || !keyboard.added)
            return;

        foreach (var key in keyboard.allKeys)
        {
            if (key.isPressed)
                Release(key);
        }
    }
}
