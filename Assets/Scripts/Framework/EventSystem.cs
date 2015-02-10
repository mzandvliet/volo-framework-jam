using System;
using System.Collections.Generic;

/*
 * When sending a message to an object we collided with, that object might be a complex object. As such, we cannot
 * be sure that interested receivers live on the actual object we collided with, but might be higher up in a hierarchy.
 * 
 * We need to deal with this.
 * 
 * We can say: look, that receiver knows beforehand that it wants collisions messages from legs, arms, and so on, and
 * take steps to ensure it has access to those messages.
 * 
 * Then when the actual collision happens handling it is simple.
 * 
 * The global event bus system is a very poor solution for simple A->B unicast.
 * 
 * If we have a collision between two objects, it would be pretty silly to let object A put a message on the bus
 * so that object B can know about what happened, right? Or is it? Maybe it is not that crazy.
 * 
 * Filtering becomes important there though. If you have a 1000 collision messages each frame, and each object is
 * only interested in collisions involving itself, then only 1/1000 messages are relevant to each object.
 * 
 * But it would be nice to be able to write: collidedObject.Post(Damage(15)) without having to know anything
 * more about potential receivers.
 * 
 * So maybe separate the problem into two parts:
 * - Interface for creating/receiving messages that is less rigid than dotnet events
 * - System for global message routing that can be used in case multi-cast/any-cast is required.
 * 
 * I'm not opposed to unicast events using the global event bus. It makes it easier to add behaviour later. But we
 * should take care it is easy to write, and performs well. Dotnet events are nearly as fast as a function call.
 * 
 * 
 * Suitable for global:
 * - Player Respawned (many subsystems do their own thing, like camera)
 * - Simulation Paused
 * - Damage Dealt, Enemy Killed (can have different effects based on game type/state implementation)
 */

public class EventSystem {
//    private IDictionary<T, IList<Action<T>>  _listeners; 
//
//    public void Emit<E>(E e) {
//        
//    }
//
//    public void Listen<E>(Action<E> listener) {
//        
//    }
}
