using System;
using RainyDayBakery;

public static class BakeryModelChecks
{
    static void Check(bool condition,string name){if(!condition)throw new Exception("Failed: "+name);}
    static BakeryModel Stocked()
    {var m=new BakeryModel();foreach(string id in BakeryModel.ItemIds)m.Add(id,20);return m;}
    static void Dough(BakeryModel m,int recipe)
    {Check(m.Mix(recipe),"Mix accepted");for(int i=0;i<3;i++)Check(m.Knead(),"Knead accepted");m.Tick(20);Check(m.PutInOven(),"Oven accepts proofed dough");}
    public static void Run()
    {
        var empty=new BakeryModel();Check(!empty.Mix(0),"Missing ingredients rejected");Check(empty.data.stage==WorkStage.Empty,"Failed mix preserves stage");
        Check(!empty.PutInOven(),"Empty oven blocked");Check(empty.Serve(false)==0,"Cannot serve without bread");
        var m=Stocked();int flour=m.Count("flour");Check(!m.Mix(6),"Locked recipe rejected");Check(m.Count("flour")==flour,"Locked recipe does not consume stock");
        Check(m.Mix(2),"Starter recipe available");Check(m.Count("flour")==flour-1,"Consumes one flour");Check(!m.Mix(0),"One batch at a time");
        m.Knead();m.Knead();Check(m.data.stage==WorkStage.Mixing,"Needs three kneads");m.Knead();Check(m.data.proofSeconds==12,"Rain proofing lasts twelve seconds");
        m.data.sky=2;m.Tick(6);Check(m.data.stage==WorkStage.Proofing,"Changing weather preserves batch timing");Check(!m.PutInOven(),"Unproofed dough blocked");
        m.Tick(6);Check(m.data.stage==WorkStage.Dough,"Proofing completes");Check(m.PutInOven(),"Oven load");m.Tick(m.GoldenStart);Check(m.TakeOut()=="Keemasan","Golden lower bound");
        m.data.sky=0;Check(m.Serve(false)==17,"Matching golden reward");Check(m.data.served==1&&m.Unlocked(1),"Delivery unlocks croissant");Check(m.Serve(false)==0,"No double delivery");
        var soft=Stocked();Dough(soft,0);Check(soft.TakeOut()=="Lembut","Early bread accepted");Check(soft.Serve(false)==8,"Alternative soft bread earns reward");
        var late=Stocked();Dough(late,2);late.Tick(1000);Check(late.TakeOut()=="Terlalu matang","Late bread remains usable");Check(late.Serve(false)==13,"Late matching bread earns reward");
        var edge=Stocked();Dough(edge,2);edge.Tick(edge.GoldenStart+4);Check(edge.TakeOut()=="Keemasan","Golden upper bound inclusive");
        var fog=Stocked();fog.data.sky=1;Check(fog.TalkAruna(),"Fog starts story");Check(!fog.TalkAruna(),"Story starts only once");
        Check(fog.Mix(7),"Story recipe initially available");fog.Knead();fog.Knead();fog.Knead();Check(fog.data.proofSeconds==6,"Fog proofing six seconds");fog.Tick(6);fog.PutInOven();Check(fog.GoldenStart==10,"Fog changes baking target");fog.Tick(10);fog.TakeOut();fog.Serve(false);Check(fog.data.story==1,"Generic customer cannot finish Aruna story");
        Dough(fog,7);fog.Tick(fog.GoldenStart);fog.TakeOut();fog.Serve(true);Check(fog.data.story==2,"Aruna delivery unlocks coastal store");
        var all=Stocked();for(int i=0;i<5;i++){Dough(all,0);all.TakeOut();all.Serve(false);}for(int i=0;i<8;i++){Check(all.Unlocked(i),"All recipes unlock after five deliveries");Dough(all,i);all.Tick(all.GoldenStart);all.TakeOut();all.Serve(false);}Check(all.data.baked.Count==8,"Eight unique breads collected");Check(all.data.discovered.Count==10,"Ten unique ingredients discovered");
        var saved=Stocked();Dough(saved,0);saved.Tick(3);var restored=new BakeryModel(saved.data);Check(restored.data.stage==WorkStage.Baking&&restored.data.bakeSeconds==3,"In-progress batch restored");restored.Tick(0);Check(restored.data.bakeSeconds==3,"Paused timer unchanged");
        var invalid=new BakeryModel(new BakerySave {sky=500,batchRecipe=-1,day=-10,stage=(WorkStage)99});Check(invalid.data.sky==2&&invalid.data.batchRecipe==0&&invalid.data.day==1&&invalid.data.stage==WorkStage.Empty,"Invalid save values sanitized");
    }
}
