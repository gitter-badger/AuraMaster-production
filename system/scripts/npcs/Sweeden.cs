//--- Aura Script -----------------------------------------------------------
// Deian in Tir Chonaill
//--- Description -----------------------------------------------------------
// Shepard Boy.
//---------------------------------------------------------------------------

public class Sweden : NpcScript
{
	public override void Load()
	{
       SetName("Chibi Sweden");
        SetRace(10002);
        SetBody(height: 0.01f);
        SetFace(skinColor: 20, eyeType: 4, eyeColor: 1, mouthType: 2);
        SetStand("human/female/anim/female_natural_stand_npc_dilys");
		SetLocation(1, 13146, 38727, 106);
		
        EquipItem(Pocket.Face, 4900, 0x00FFDC53, 0x00FFB682, 0x00A8DDD3);
        EquipItem(Pocket.Hair, 4009, 0xE8CD00);
		EquipItem(Pocket.Head, 18029);
        EquipItem(Pocket.Armor, 15252, 0x000000, 0x0000FF, 0xFFFFFF);
        EquipItem(Pocket.Shoe, 17021, 0x000000);
		EquipItem(Pocket.RightHand2, 40348, 0x00755748, 0x005E9A49, 0x005E9A49);
		EquipItem(Pocket.LeftHand2, 40348, 0x00755748, 0x005E9A49, 0x005E9A49);
		
        AddPhrase("Dude...Bring me a beer");
        AddPhrase("HEY GURL DAYUM YOU LOOK FINE");
        AddPhrase("*Drinks beer*");
        AddPhrase("DONT RUN AWAY I WAS JUST MESSING WITH U");
        AddPhrase("Im Bored");
        AddPhrase("Wonder what Swe is doing");
        AddPhrase("Bwah");
        AddPhrase("Dude...No...");
        AddPhrase("THATS WHAT SHE SAID.");
	}

	protected override async Task Talk()
	{
		SetBgm("NPC_Deian.mp3");
	
		await Intro(
			"An adolescent boy carrying a shepherd's staff watches over a flock of sheep.",
			"Now and then, he hollers at some sheep that've wandered too far, and his voice cracks every time.",
			"His skin is tanned and his muscles are strong from his daily work.",
			"Though he's young, he peers at you with so much confidence it almost seems like arrogance."
		);

		Msg("What can I do for you?", Button("Start a Conversation", "@talk"));

		switch (await Select())
		{
			case "@talk":
				Msg("What did you say your name was?<br/>Anyway, welcome.");
				await StartConversation();
				return;
				
			default:
				Msg("...");
				break;
		}
	}

	protected override async Task Keywords(string keyword)
	{
		switch (keyword)
		{
			case "personal_info":
                Msg("I'm Denmark and I am king of scandinavia");
                //Msg("Once again, welcome to Tir Chonaill.");
                // MoodChange
                break;

            case "rumor":
                Msg("I heard Swe is marrying Finland...");
                //Msg("Talk to the good people in Tir Chonaill as much as you can, and pay close attention to what they say.<br/>Once you become friends with them, they will help you in many ways.<br/>Why don't you start off by visiting the buildings around the Square?");
                // MoodChange
                break;

            case "about_skill":
                Msg("HAHAHA, AXE SKILL IS BEST SKILL!");
                //Msg("You know about the Combat Mastery skill?<br/>It's one of the basic skills needed to protect yourself in combat.<br/>It may look simple, but never underestimate its efficiency.<br/>Continue training the skill diligently and you will soon reap the rewards. That's a promise.");
                break;

            case "about_arbeit":
                Msg("You could start by getting me a beer");
                //Msg("Are you interested in a part-time job?<br/>It's great to see young people eager to work!<br/>To get one, talk to the people in town with the 'Part-Time Jobs' keyword.<br/>If you go at the right time, you'll be offered a job.<p/>If you do a good job, you will be duly rewarded.<br/>Just make sure to return to the person who gave you the job and report the results before the deadline.<br/>If you miss the deadline, you will not be rewarded regardless of how hard you worked.<p/>Part-time jobs aren't available 24 hours a day.<br/>You have to get there at the right time.<p/>The sign-up period usually begins between 7:00 am and 9:00 am.<br/>Since there are only a limited number of jobs available,<br/>others may take them all if you're too late.<br/>Also, you can do only one part-time job per day.<p/>It looks like Nora and Caitin could use your help,<br/>so head to the Grocery Store or the Inn and talk to them.<br/>Start the conversation with them with the keyword 'Part-Time Jobs' and make sure it's between 7 and 9 am.<br/>Good luck!");
                break;

            case "about_study":
                Msg("Who needs to study...However England been doing magicial stuff I guess.");
                break;

            case "shop_misc":
                Msg("I love beer.");
                break;

            case "shop_bank":
                Msg("The bank is just behind you dude.");
                break;

            case "skill_counter_attack":
                Msg("Of course I know about Counter attack haha.");
                break;

			default:
				RndMsg(
					"Ask all you want, I'm not telling you.",
					"Hold up, I feel like I'm being interrogated.",
					"Meh, I don't want to tell you.",
					"Pry all you like. You'll get nothing from me.",
					"So many questions, at least give me a small gift...",
					"Sometimes, I'm just not in the mood to answer questions."
				);
				break;
		}
	}
}

