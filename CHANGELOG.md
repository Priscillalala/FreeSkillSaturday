## 1.7.0
* Updated to IVYL 2.0
	* Fixes language tokens not falling back to English when using other languages
	* Fixes error spam with IVYL and HolyDLL
	* Updated codebase - now loads both assets and addressables asynchronously (should be very smooth loading)
	* Added ShaderSwapper dependency
* Split config into multiple files
	* Old config values will be lost - sorry!
* Venom reworked
	* No longer applies a stacking slow over time
	* Stuns poisoned enemies and leaves a lingering slow
* Disembowel
	* All 3 hits apply poisonous now (used to be only the third hit)
	* Slightly reduced lunge velocity
* Theremin
	* Attack speed bonus: 45% (+35% per stack) => 45% (+45% per stack)
* XQR Chip
	* Minor optimizations

## 1.6.1
* Reboot resets equipment cooldowns
* Fix reboot affecting the music of all players in a lobby

## 1.6.0
* New Alternate Skill: Reboot
* Moved image file host from discord to github in preparation for creating a wiki

## 1.5.0
* New Alternate Skill: XQR Chip
* Fix the Railgunner: Hipster achievement not triggering for clients

## 1.4.0
* New Alternate Skill: Pulse Grenade
* Removed PrefabAPI dependency
* Reworked the codebase to load asynchronously alongside addressables
	* Keep an eye out for any bugs this may have created...The mod may be unstable for a few patches!

## 1.3.1
* Fix Disembowel sharing the Venom achievement

## 1.3.0
* New Alternate Skill: Venom
* New Alternate Skill: Disembowel
* Ported to IVYL!

## 1.2.0
* New Equipment: Godless Eye

## 1.1.0
* New Artifact: Artifact of Entropy

## 1.0.0
* First release