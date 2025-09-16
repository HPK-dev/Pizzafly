# 🍕 Dough Physics Implementation

## Step 1: Dough Representation

We want the dough to behave like a floppy, stretchable disc. Instead of one solid mesh, we simulate it as a **network of physics nodes**:

* **Nodes (Rigidbodies):**

  * Imagine placing small spheres in a **circle layout** (like the outer rim of the pizza).
  * Optionally, add **interior nodes** (a smaller inner circle, or a single central node) to prevent collapsing.
  * Each node has:

    * `Rigidbody` (mass should be small, \~0.1–0.5)
    * `SphereCollider` (tiny radius, just to allow collision detection if needed)

* **Topology (Connections):**

  * Each node connects to its **neighbors** via joints.
  * The outer ring forms a loop.
  * Each node also connects inward (to the center or inner ring).

This gives us a flexible, jelly-like structure that holds together.

---

## Step 2: Physics Setup

The physics is controlled by **joints**:

* **Joint Type:** Use `SpringJoint` or `ConfigurableJoint`.

  * `SpringJoint` is simpler and already provides a stretchy-but-returning force.
  * `ConfigurableJoint` is more advanced but gives extra control (like axis limits).

* **Joint Settings:**

  * **Spring:** Controls how strongly the dough pulls back to its rest shape. Low values = floppy, high values = stiff.
  * **Damper:** Reduces jitter by absorbing excess energy. Tune so the dough doesn’t endlessly bounce.
  * **Min/Max Distance:** Prevents overstretching or collapsing.

* **Example Values:** (starting point)

  ```
  Spring = 50
  Damper = 2
  MinDistance = 0.5f * restLength
  MaxDistance = 1.5f * restLength
  ```

* **Throwing Mechanic:**

  * To grab: Attach the player’s hand to one or more nodes using a `FixedJoint` or by applying force.
  * To throw: Release the joint and apply force/impulse to those nodes. The network will follow.

---

## Step 3: Visual Mesh (Pizza Dough Mesh)

We don’t want to just see floating spheres — we want a smooth pizza dough surface.

* **Mesh Options:**

  1. **Dynamic Mesh:**

     * Create a flat circular mesh (like a subdivided plane or disc).
     * Each vertex is bound to the nearest physics node.
     * Every frame, update the vertex positions to match the physics nodes.

  2. **Skinned Mesh Renderer:**

     * Treat each node as a bone.
     * Bind the mesh with bone weights.
     * Unity’s skinning system automatically interpolates vertex positions.

  3. **Cloth Component (Optional):**

     * Unity’s `Cloth` can simulate stretchy fabric.
     * But harder to control for gameplay, so custom node-based simulation may be better.

* **Collision Handling:**

  * Keep colliders on nodes small.
  * Disable self-collision between nodes (otherwise the dough explodes).
  * For interaction with the world, let the mesh be **visual only**, while the physics nodes handle collision.

---

✅ At this stage: You’ll have a floppy disc of nodes connected by springs, and a mesh that updates to match them. This behaves like stretchy dough you can grab and throw.
