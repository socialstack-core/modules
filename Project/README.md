# My SocialStack Project

Hello! To start developing or running this project, you'll first need to make sure the dependencies are all good to go. This project is made using a highly modular open source platform called [SocialStack](https://wiki.socialstack.dev/index.php?title=Main_Page). To learn more about the dependencies that it has, install the [socialstack package on npm](https://www.npmjs.com/package/socialstack) and the dependencies listed there. 

If this is your first time using MySQL on your own computer, it is recommended to also install MySQL Workbench so you can edit the database contents. It is not the only tool for this though so otherwise feel free to use your favourite MySQL UI.

## First time setup

If you have all the dependencies installed, you'll next need to create the database. Run this in a terminal cd'd to this project and watch for the database name that it outputs:

```
socialstack init
```

Next, you'll need to populate the database. Ask someone else on the development team of this project to provide a database dump for you to use - They may have already done this in a directory called `Database` in this repository. It's just a .sql file, so open it up in e.g. MySQL Workbench and run the .sql file against the empty database that got created by the above init call. To run an sql file against a specific database in MySQL Workbench, open the localhost database connection and then double click the database on the left hand side such that it goes bold. You can then just copy/paste the database file in to the main editor.

If you missed the database name during the init call, you can check for its name in the file `appsettings.json`.

## Running the project 🏃‍♀️

Next, open the .csproj file in your favourite C# editor. This is usually Visual Studio (the Community version is fine) but can also be other things like VS Code where you can then start the project. In Visual Studio, it is by pressing the play button at the top of the UI, or F5. 

If you're not going to be editing the C# at all and just want to run the project without a full IDE, for example because you only want to edit the javascript for the UI, you can also open a terminal cd'd to this directory and run this command (Windows, Mac or Linux):

```
dotnet run
```

It will build the .csproj and then start it up for you.

Regardless of which route you used to run the project, you'll then see a console window appear which will display the output of the running API. Everything started successfully when you see:

```
Done handling UI changes
```

This indicates the database connected successfully, the C# built and is running, and the UI javascript built and is running. The output will also indicate how to view the site that you have running too, like this:

```
Ready on 0.0.0.0:5050
```

If you have never seen 0.0.0.0 before, it is the IP that represents *"any IP address this computer responds to"*. Some projects will explicitly state `Ready on 127.0.0.1:5050` instead. The any IP means you can view the site using your computers LAN IP on a real phone, or localhost in a web browser, such as http://127.0.0.1:5050/

At this point, editing any of the javascript files using your favourite editor will trigger an automatic near instant UI build and a page refresh if you have the site open.

# Collaboration guide 🤗

So you've got the code of the project running - awesome! Now what? Well, here's some tips on how we typically write code so we can keep things flowing smoothly.

## Git commit strategy 💻

This project uses the "master branch with feature flags and feature branches" commit strategy. This is similar to what Google does although each project is in its own repository rather than their monorepo strategy. This means you typically commit to the master branch. If you are adding a new feature then it is added behind some sort of configuration option and is off by default.

* Configuration options use the [service Config options in SocialStack C#](https://wiki.socialstack.dev/index.php?title=Configuration).
* Configuration options use React component props in javascript or [Frontend] marked options from C# (above).

If you are editing existing code which is live (e.g. it's behind a feature flag which is active on the production and/or stage site) and you are not entirely confident with the change, put your commit in a feature branch. Another developer can then check it over and optionally set the stage site to load your branch instead of master. When using feature branches, please use the rebase merge method. Feature branches should also be named using the ticket number and some nice descriptive short name too, for example, "1121-mapping-ui-crash-fix".

Your local branching strategy is yours to choose. For example some people like to have multiple local branches which then never actually get pushed to the repository.

The aim of this strategy is to minimise merge overhead whilst also keeping the master branch in a deployable state at all times.

All commits should also contain the ticket number at the start of the description with a hash, for example, "#1121 Fixes a crash on the mapping UI".

**Tip:** It is highly recommended to commit small and commit frequently, and pull before each commit. You don't need to push all commits each time though. This helps greatly with time tracking as your commit log gives you a historical record of when you were working on tickets. Larger gaps are often then meetings or Teams chats which can be found in the Teams call history.

## Code up, content down

"Huh?" Great, I'm glad you asked. **Code up, content down** is a content management strategy. It is important to heavily content based platforms like Wordpress or, in this case, SocialStack. For example, a new website page is database content and needs to be added to the production site as well as stage - and that needs to happen alongside new pages, blogs etc, being added by the content team on the live site.

>  Code changes you make flow "up" from your computer, to stage, to the production site.
>
> Content changes flow "down" from production, to stage, to your PC.

Typically in practise this means you'll be frequently pulling database backups from the stage site to make sure that content you're creating, such as new pages, is working nicely with the rest of the project content. You can of course create pages directly on your local site but it's suggested to create empty placeholder ones on production first such that it all lines up and you don't end up forgetting about it. 

Due to size constraints on larger projects, local development is sometimes a subset of the database on stage. This is handled automatically for you when you click the database dump button on SocialStack Cloud to grab the latest one.

## Code formatting 💅

* Please make sure your editor is set to save as tabs rather than spaces when working with both C# and javascript. You're very welcome to hit the spacebar but just make sure committed code contains tab formatting. This avoids "every line has changed" commits with files oscillating between tabs and spaces.

* Any C# is formatted using the Microsoft default style ([Allman](https://en.wikipedia.org/wiki/Indentation_style#Allman_style)). This really just means you don't need to edit any config in a default C# editor such as Visual Studio.

* Javascript is formatted using the [K&R-Java variant](https://en.wikipedia.org/wiki/Indentation_style#Variant:_Java), also the most widely used formatting style for javascript so again editors on default settings will typically output this style anyway.

* Please use the "mandatory braces" guideline. This helps avoid famous problems like Apple's ironically named [goto fail bug](https://www.synopsys.com/blogs/software-security/understanding-apple-goto-fail-vulnerability-2/). This guideline simply means to always use brackets with conditional statements, including when they are optional in the language:

```javascript
if(javascriptThingIsTrue)
	doThing(); // This is valid but does not follow the guideline

if(javascriptThingIsTrue) {
	doThing(); // Follows the guideline
}
```
