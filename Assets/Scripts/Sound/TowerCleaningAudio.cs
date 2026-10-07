using UnityEngine;

// Original synthesized cleaning sounds. No downloaded or licensed audio assets.
public static class TowerCleaningAudio
{
    private static readonly System.Collections.Generic.Dictionary<string,AudioClip> clips = new();
    public static void Install()
    {
        var audio=GameAudioManager.Instance; if(audio==null) return;
        audio.shootSFX=Make("PressureSpray",0.12f,1600f,0.7f);
        audio.splashBombSFX=Make("FoamBurst",0.38f,170f,0.85f);
        audio.enemyDeathSFX=Make("DirtDissolve",0.22f,460f,0.45f);
        audio.cardSelectSFX=Make("ElevatorChime",0.3f,880f,0.02f);
        audio.bossPhaseSFX=Make("MachineWarning",0.65f,280f,0.12f);
        audio.bossDeathSFX=Make("MachineShutdown",1.1f,130f,0.55f);
        audio.playerHitSFX=Make("ShieldClank",0.17f,210f,0.4f);
        audio.emergencySFX=Make("FilterWarning",0.25f,650f,0.08f);
    }

    private static AudioClip Make(string name,float seconds,float frequency,float noise)
    {
        if(clips.TryGetValue(name,out var existing) && existing!=null) return existing;
        const int rate=22050; var samples=new float[Mathf.CeilToInt(rate*seconds)];
        var random=new System.Random(name.Length*7919); float filtered=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=(float)i/rate, p=t/seconds;
            float n=(float)random.NextDouble()*2-1; filtered=filtered*0.65f+n*0.35f;
            float tone=Mathf.Sin(2*Mathf.PI*frequency*(t-0.28f*t*t/seconds));
            float envelope=Mathf.Min(1,t/0.006f)*Mathf.Pow(1-p,2);
            samples[i]=(filtered*noise+tone*(1-noise))*envelope*0.45f;
        }
        var clip=AudioClip.Create(name,samples.Length,1,rate,false); clip.SetData(samples,0); clips[name]=clip; return clip;
    }
}
