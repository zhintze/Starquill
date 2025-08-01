- Each stat **counters two others**.
  
- Each stat is **countered by two others**.
  
- **No mutual counters** (e.g., INT beats WIS but WIS does *not* beat INT).
  
- Attribute relationships feel **thematically justified**.
  
- Spread of physical, mental, and social is **balanced**.
  

| Attribute | Beats | Beaten By | Logic |
| --- | --- | --- | --- |
| **STR** | CON, CHA | DEX, INT | Brute force breaks defenses & presence, but is outmaneuvered an outwitted |
| **DEX** | STR, CHA | CON, WIS | Speed beats force & charm, but is foiled by awareness and durability |
| **CON** | DEX, INT | STR, CHA | Endurance shrugs off finesse & intellect, but is vulnerable to raw force & manipulation |
| **INT** | STR, CHA | CON, WIS | Strategy beats brawn & talk, but fails against toughness & insight |
| **WIS** | DEX, INT | CHA, STR | Perception outplays speed & intellect, but can be misled or overwhelmed |
| **CHA** | WIS, CON | DEX, INT | Influence beats resolve & stubbornness, but is dodged and deconstructed |

## Formula
**Attacker Roll:** d20 + [Attacker's Chosen Attribute Modifier] + [Traits/Equipment Modifier]

vs.

**Defender Roll:** d20 + [Defender's Highest Opposite Attribute Modifier] + [Traits/Equipment Modifier]

## Example Scenario:

- Attacker (using INT Verb: "Exploit Weakness")
  
- INT Modifier: +4
  
- Roll: d20(13) + INT(+4) = 17 total
  
- Defender checks if WIS is higher than DEX (WIS counters INT).
  
- Defender's WIS Modifier: +2
  
- Roll: d20(10) + WIS(+2) = 12 total
  

## Handling Edge Cases Clearly:

**Multiple Counter Attributes:** Defender always chooses their strongest available counter-attribute.

**Natural 20 or 1:**

- **20:** Critical Success, applies additional effects.
  
- **1:** Critical Fail, introduces negative effects or vulnerabilities.
  

## Attribute Modifiers

modifier = (score - baseline) // scale

**So:**

- A stat of **10 or 11** = no modifier
  
- A stat of **16** = +3
  
- A stat of **7** = -2
  

# Potential Suggestions

### **Dynamic Baseline Based on Global Stat Averages**

party_avg = sum(p.score for p in party) / len(party)
baseline = party_avg * avg_party_score

- start with 75% average party score for testing
  
- Tracks how powerful the party is and adjusts baseline accordingly
  
- Keeps challenge relative, avoids runaway stat inflation
  
  - scale = 2 # For example: every 2 points = +1 modifier
    modifier = clamp((score - baseline) // scale, min_mod, max_mod)
    
  - min_mod = -5
    max_mod = +5
    
  - enemy_baseline = party_avg - enemy_advantage_margin
    
  - enemy_advantage_margin = 3 * scale # e.g. 3 modifier steps = 6 pts
    

## **Verb Cooldowns or Fatigue**

- Implement limited or cooldown-based defensive verbs.
  
- Consecutive use of the same defensive verb reduces effectiveness temporarily.
  

**Example:**

- "Brace" (CON verb) effectiveness decreases by -1 per repeated consecutive use.

**Impact**:  
Encourages rotating through verbs, preventing spamming high-defense actions.

## **Equipment and Trait Impact**

With large stat numbers, **+50 to a stat** can either mean:

- Nothing (if the scale is bloated), or
  
- Everything (if modifiers are sensitive)

So ensure:

- Equipment gives **% boosts or modifier boosts**, not flat stat gains (e.g. “+1 to STR modifier”, not “+300 STR”).
  
- Traits adjust **effective tier or threshold**, not raw numbers.
  
