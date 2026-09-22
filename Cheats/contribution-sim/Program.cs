using System;using System.Collections.Generic;
static class P{
    internal static int Bracket(int v) => v / 100; // contribution unlock thresholds are multiples of 100
    // Native model (0xB78FA0): positive delta may first become RoundToInt(culture% * delta * 0.01f) when the player
    // stays in a matching culture (point 133); then gain = (int)(((float)rate21 / 100f + 1f) * delta); result clamped 0..1000.
    internal sealed class Model {
        internal float Rate = 1f; internal int? Culture; internal bool CultureKnown;
        internal int Plain(int d) => (int)(Rate * d);
        internal int WithCulture(int d) => (int)(Rate * (float)Math.Round((float)(Culture!.Value * d) * 0.01f, MidpointRounding.ToEven));
        internal IEnumerable<int> Results(int cur, int d) {
            if (d > 0 && Culture.HasValue && Culture.Value != 100) {
                if (!CultureKnown || CultureApplies) yield return Math.Clamp(cur + WithCulture(d), 0, 1000);
                if (!CultureKnown || !CultureApplies) yield return Math.Clamp(cur + Plain(d), 0, 1000);
            } else yield return Math.Clamp(cur + Plain(d), 0, 1000);
        }
        internal bool CultureApplies;
    }
    // Picks the delta whose every possible result is closest to the target without moving into a different
    // 100-bracket than the start or the target (crossing a threshold and coming back would grant or revoke unlocks
    // the player did not ask for). Deltas with no effect are skipped.
    internal static int Choose(Model m, int cur, int target) {
        int best = Math.Sign(target - cur), bestDist = int.MaxValue;
        for (int k = 1; k <= 1200 && bestDist > 0; k++)
            foreach (int d in new[] { k, -k }) {
                int dist = 0; bool allowed = true, moves = false;
                foreach (int v in m.Results(cur, d)) {
                    if (v != cur) moves = true;
                    bool between = v >= Math.Min(cur, target) && v <= Math.Max(cur, target);
                    bool away = (v - cur) * (target - cur) < 0;
                    if (!(between || Bracket(v) == Bracket(target) || (away && Bracket(v) == Bracket(cur)))) { allowed = false; break; }
                    dist = Math.Max(dist, Math.Abs(target - v));
                }
                if (!allowed || !moves) continue;
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
        return best;
    }

 static int Native(int cur,int d,int rate,int? cult,bool cond){
   if(d>0&&cult.HasValue&&cond) d=(int)Math.Round((float)(cult.Value*d)*0.01f,MidpointRounding.ToEven);
   int g=(int)(((float)rate/100f+1f)*d); if(cur+g>1000) g=1000-cur; if(g==0) return cur;
   return (cur>0||g>=0)?Math.Max(cur+g,0):cur; }
 static void Main(){ int bad=0,cross=0,maxc=0,tot=0;
  foreach(var rate in new[]{0,5,10,20,35,50,80,99,-20,-50})
  foreach(var cult in new int?[]{null,110,150,200,80,125})
  foreach(var cond in new[]{true,false})
  foreach(var before in new[]{0,1,37,100,150,500,999,1000})
  foreach(var target in new[]{0,1,2,50,99,100,101,333,999,1000}){
   if(before==target) continue; tot++;
   var m=new Model{Rate=(float)rate/100f+1f,Culture=cult}; int cur=before,calls=0;
   while(cur!=target&&calls<8){ int d=target==1000?2000:target==0?-2000:Choose(m,cur,target);
     int now=Native(cur,d,rate,cult,cond); calls++;
     if(d>0&&!m.CultureKnown&&m.Culture.HasValue&&m.Culture.Value!=100&&now<1000){int w=Math.Clamp(cur+m.WithCulture(d),0,1000),p=Math.Clamp(cur+m.Plain(d),0,1000); if(w!=p&&(now==w||now==p)){m.CultureKnown=true;m.CultureApplies=now==w;}}
     int lo=Math.Min(before,target),hi=Math.Max(before,target);
     if(Bracket(now)!=Bracket(target)&&!(now>=lo&&now<=hi)&&Bracket(now)!=Bracket(before)) cross++;
     cur=now; }
   if(cur!=target){bad++; if(bad<5)Console.WriteLine($"miss rate={rate} cult={cult} cond={cond} {before}->{target} got {cur}");}
   maxc=Math.Max(maxc,calls);}
  Console.WriteLine($"cases={tot} bad={bad} crossings={cross} maxCalls={maxc}");}}
