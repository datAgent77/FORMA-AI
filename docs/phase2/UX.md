# First-use experience and design decisions

This is an authored Unity spatial UI implementation, not a validated premium headset experience. No web mockup or synthetic screenshot is offered as proof of operation.

## Visual system
The studio uses an ocean training palette: deep slate #0E1A26 for a compact panel, ocean #217D82 for controls, pearl #F0F5F0 for text, and sand #E6BA75 for the original Sol coach accent. Mira uses slate blue with pale sea-glass accents; Atlas uses muted violet with silver-blue accents. These distinguish original development characters without celebrity likenesses or external assets.

Text uses Unity's built-in LegacyRuntime font to avoid a new licensed font dependency. Title/body/control hierarchy uses 0.043/0.025/0.020–0.024 TextMesh character sizes at a nominal 2.4 m viewing distance. These are starting parameters, not validated angular legibility. Review Turkish glyphs, line length, contrast and readability on Quest 3 and 3S. Replace the development font only after reviewing the actual licensed asset and rendering costs.

A single eye-relative panel owns the current step; it does not follow every head movement. Place panel in front is an explicit user action. One primary action, a Back action, and an optional Skip action preserve hierarchy. Safety is never skippable. No inactive Worlds page, subscription offer or fake checkout is shown. Broader Home/Workouts/My Coach/Worlds/Progress/Settings navigation belongs to later working modules rather than empty destinations.

## Welcome
Two short sentences establish a gentle workout and visible physical room. The user chooses seated/standing position, mute/volume, reduced idle motion and setup language. There is no audio clip or spoken output yet; audio preferences change Unity's audio listener volume and are retained, without implying speech availability. Input is explicitly head gaze + Touch trigger; B/Y pauses through the existing Studio controller. Hands are not advertised.

## Coach
Cycle original Sol/Mira/Atlas appearances and choose Motivator/Calm Guide/Technical Coach independently. A deterministic text preview changes phrasing. Voice is explicitly text-only, and no microphone permission is requested. The same core exercise/safety cues remain in force for every personality. Procedural color/accessory looks and gentle idle are development art; natural character motion, speech/listening animation and polished demonstrations are not complete.

Move coach cycles three eye-relative lateral positions at 2.6 m distance and invalidates space confirmation. This is manual placement; it does not know about furniture/walls. Inspect the physical surroundings before confirming. Repositioning is blocked during a workout. The miniature character is intentionally stylized; it does not establish form accuracy.

## Optional profile
No name, age, weight, medical history or free-text diagnosis is requested. Goal, experience, duration and movement-type fields store planning preferences only; they do not change the fixed Phase 1 session dose. Equipment is an inventory preference and never authorizes equipment use. Optional avoid-lower-body/avoid-shoulders flags filter available starter movements. Skip optional fields clears those elective values. A user who voluntarily selects exclusions is never automatically told that the remaining exercise is medically suitable.

## Safety and review
Require both an acknowledgement of stop conditions and a fresh physical-space confirmation. Focus loss, suspension or manual repositioning invalidates confirmation. Review explains the 1–3 minute fixed equipment-free starter, text-only voice, language limits and any incompatible movement selection. Saving must succeed before entering Studio; retry remains possible without losing the in-memory draft. Physical-space acknowledgement is not persisted as standing permission to exercise.

## Studio handoff
A returning completed setup enters Studio, but every workout still requires the original per-session safety gate. English/Turkish apply to setup only; the UI discloses that Phase 1 workout cues remain English. Seated mode selects seated shoulder-roll instructions and excludes standing march. If seated/limitation preferences leave no catalog movement, start is blocked and the user is directed to edit preferences. No AI-derived alternative is fabricated.

The existing history panel exposes Edit coach and preferences, Move coach and Export setup profile. A workout must be stopped before editing; hiding an active session behind setup is not allowed. Existing pause/end/exit controls remain available. Export writes local JSON for developer-assisted ADB retrieval; a consumer sharing UI is not implemented.

## Required usability work
Test comfortable reach/angles, panel occlusion, Turkish font coverage, seat height, coach placement, eye strain and button sizing on both headsets. Provide a validated hand-select adapter and accessible pause before claiming hands support. Finish real character assets/audio, translated workout safety text, production navigation and a consumer export path before commercial release. Screenshots must come from actual Unity or headset execution; none was available on this host.
