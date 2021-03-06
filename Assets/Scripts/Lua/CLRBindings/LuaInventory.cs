﻿using UnityEngine;
using System.Collections;

public class LuaInventory {
    public string GetItem(int index) {
        if (index > Inventory.container.Count) {
            UnitaleUtil.displayLuaError("Getting an item", "Out of bounds. You tried to access the item n°" + index + 1 + " of your inventory, but you only have " + Inventory.container.Count + " items.");
            return "";
        }
        return Inventory.container[index-1].Name;
    }

    public void SetItem(int index, string Name) { Inventory.SetItem(index-1, Name); }

    public bool AddItem(string Name) { return Inventory.AddItem(Name); }

    public void AddCustomItems(string[] names, int[] types) { Inventory.addedItems = names; Inventory.addedItemsTypes = types; }

    public void SetInventory(string[] names) { Inventory.SetItemList(names); }

    public bool NoDelete {
        get { return Inventory.usedItemNoDelete; }
        set { Inventory.usedItemNoDelete = value; }
    }

    public void SetAmount(int amount) {
        Inventory.tempAmount = amount;
    }
}

/*So, here we begin
We'll take this class as an example
Tell me what you wanted to say
I just clicked back to the window 
:PX)
Bad start
Erm... First of all, all C# scripts are contained in classes (sorry for the wordreference)
np :)
There is a big difference between Lua and C# : Lua is a compiled language (that means that it don't need classes and such) and C# is a class-oriented language (it need classes)
Understood ?
somewhat; let's start with what classes actually do
Classes are objects that contains functions and variables. It's like an entire Lua script.
I see
Here, we can see these keywords : public (that means that the class is accessible from everywhere) and class, and then the class name.
Here, you'll not use "end" and "then" keywords : it's only { and }.
The good thing with Visual Studio is that it corrects all your mistakes, example
Don't worry about making mistakes, these red bastards will help you a lot :P
So they act similar to grammar mistakes in regular text editors
Exact
Plus, C# variables are initialized with this "schema" : TYPE name = (new) value. The new keyword is used when you initialize complex variables, such as arrays or custom classes.
UNderstood ?
I think a concrete example will help here; I understand regular variables and how the type stuff works I think
Okay, so I4ll ask you something : create a string named myFirstSentence with "yay" as value. :P
anywhere I assume
IN Something()
Sir yes sir!
I almost forgot, C# uses semicolons x)
Yes, I just remembered that :P
Visual Studio helps a lot with this kind of mistakes :P
brb
kk
back
kay, I'm here too
Okay, so you're good for now :3
MAy we try something harder ?
let's discuss what void does for example
void is the return type of the function : you can replace it by int, float, string, LuaINventory etc..
As the function returns nothing, the return type is "void"
ok, so if I understand correctly, everything that is 'void' will run, but not return, and everything that is not will?
Eveything that is not "void" will need a return value!
Example :
Here it works, 
:P
There is two types of classes : normal classes, that need to be instantiated to be used, like LuaInventory
And there's another class type, "static" classes, like Inventory.
We can use it directly, everywhere, at every moment
Do you see now where you're going ?
I'm starting to grasp it
  
To sum-up :
- Classes are the basis element of C# coding.
- There is two type of classes : normal classes that needs an initialization to be used and static classes that can be used directly from everywhere.
- Variables follows this schema : TYPE name = (new) value (new is needed for complex variables)
- Function has return types, like 'int' or 'LuaInventory'. You can replace it per 'void' if you want the function to return nothing

Is it more clear now ?
C# is like boxes in boxes, as you can see here (nvm for the search)
ok, this boxes in boxes idea works well with me; I already use functions within functions left and right in Unitale, and require libraries all the time; it's somewhat like that
Yup
So, let's get to the point. Normally, you'll need one file for each class, but as it is an example, you'll use this file.

Can you make a normal class, with two functions called "DoesAThing" and "HANDZ" : the first returns nothing and the second an object "Handz", 
one variable called "yes" that is an int and "no" that is a boolean. Here you go!
The nae of the class will be "ANormalCClass"

one final question: by object we mean? - I'll probably jump around a lot at first; and it will be a public class
Object  = class, do as I did with variables (LuaInventory luai = ...) LuaInventory is an object/class
Check your errors, red things are your friends
ah!
???
it appears I'm still confused with returning an object
You can't make classes in classes. I can show it to you, but it'll not be nice
Just show me an example where it is done and I'll go from thre
Here you go!
AH! I get it now; I think :D
ok, so I know how to return a single value, that's no problem, but I still have difficulty understanding how to return two different types
Wait, I'll do sth
Do I have to show you the answer ?
So, here's the answer

Here's your class! Good job!
I think I must have misunderstood the task it seems
Yeah, I must havebeen more clear about that, soory :/
I thought I had to put
I know, but if I wanted you to do so, I'd have said : and put two arguments...
But hey, that was a great start ^^ i have to go eat
yes and no in to Handz

public class Handz { }

public class ANormalCClass {
    int yes;
    bool no;
    public string Something() {
        string myFirstSentence = "yay";
        return myFirstSentence;
    }
    public void DoesAThing() { }

    public Handz HANDZ() {
        return new Handz();
    }
}*/

/*
Hey
h0i, it's so smooth :D
200Mb/s man :P
Okay, so
We'll start with unity's basic elements
let me know if you need confirmation at any point
kk
Unity is organised by some basic elements : we'll start with the most basic element : Scenes
Scenes are...like...a graphic space where you put other elements : 
you can create scenes, load them one by one or load some asynchronously...
In this case, my scene is named "TransitionOverworld", but ig i'll call other scenes.
Example : now I'm on test2
Understood ?
So far so good.
Okay
I'll show you these elements, and then you'll try to create your first scene
Is the music perturbing you ?
actually, I turned off sound last time :D
D:
Now we're on ModSelect etc...
Okay, next step
So, these scenes contains other elements : GameObjects
On Unity, we can see them here
We can see that GameObjects works in an hierarchy : 
Canvas contains all GameObjects under it
I don't think that you have a gameObject limit, but don't put too much 
gameObjects, or it'll not be memory-friendly
The gameObjects have 2 types : active and inactive.
IN this case, FightUI is an inactive gameObject, and TextManager is active
An inactive gameObject doesn't intefere with the game.
Do I have to repeat myself ?
I don't think so, so far it's quite simple to grasp.
Unity is like "boxes in boxes" (again)
Project --> Scenes --> GameObjects --> Components
And here we go for the components
So, in the inspector, we can see the different components of a GameObject.
For example, Background contains 4 components : a RectTransform, 
a Canvas Renderer, a Background Loader and an Image.
These components can be active or not active too, as the switch tells us 
next to the component's name
(These logs modify the project, so I have to reload the project each time 
I start some tests)
So, as we can say, if I go to Background and I disable the Image component...
No more background :P
gasp, I have to say
And if I enable it again, the background returns!
There is lots and lots of Component types, and you can even create your own types! 
But we'll see that later :P
Lots and lots of types
I think this is the kind of thing that can be learned in depth
from a documentation as well (the functionality of these types that is)
Yeah
I'll just tell you the principal types, the ones that you'll always need to use
When you create a GameObject, this one always have at least one component : Transform
This component is here to tell Unity where the object is placed on the screen
Your created component is this little circle on the scene, between the enemies
As it is nothing for now, it does nothing and appears as a circle
Sometimes, when you create gameObjects in a given gameObject, this component changes : 
now we have a RectTransform, and the GameObject is now displayed as a rectangle on the 
screen
A rectTransform is an advances Transform where you can edit things such as the width 
and height of the gameObject. It replaces Transform.
Do you get it now ?
Yup.
Nice ^^ If you want to, we can go to the next chapter : II - Unity's behaviour (C# part)
let's do that then :)
Okay ^^
So, now we'll go on Visual Studio.
There is some special functions that are autotriggered in your class : here's some examples.
*/

/*public class Handz { }

public class ANormalCClass {
    int yes;
    bool no;
    public string Something() {
        string myFirstSentence = "yay";
        return myFirstSentence;
    }
    public void DoesAThing() { }

    public Handz HANDZ() {
        return new Handz();
    }

    public void Start() {
        //This function will be autotriggered when the class with be instancied
    }

    public void Update() {
        //You already know this function, don't you ?
    }

    public void OnApplicationQuit() {
        //Another example, when we close the game
    }
}*/

/*
You get it ?
These are from Unity I take.
Okay
So, now we'll REALLY need to create another file
*/