# Smoke checklist

A manual check, run on every release build, that one full in-game Day plays start to finish on each control scheme
(REQ-13; technical-blueprint.md §6). Play each pass with only that device: do not touch the other one at all. Tick a
box only when the step works and every prompt on screen names that pass's buttons.

To reach a harvest on the Day you play, load a save that has a ripe crop in the field, or keep playing the same save
until a crop ripens and finish the remaining steps on that Day.

Button names follow the glyphs the game shows (controller labels are Xbox-style, as Steam Input presents them on the
Steam Deck).

## Controller-only pass

Buttons: move `(L stick)` (or D-pad), run `(B)`, use `(A)`, interact `(X)`, previous item `(LB)`, next item `(RB)`,
next hotbar row `(View)`, open the backpack `(Y)`, pause `(Start)`, close a menu `(B)`, turn the camera `(R stick)` left and right, zoom the camera `(R stick)` up
(in) and down (out); menus move with the D-pad or `(L stick)` and press with `(A)`.

- [ ] Before you begin, the mouse cursor is hidden once the controller is used, and stays hidden for the whole pass.
- [ ] Title screen: start a new game, or load a saved game, with the D-pad and `(A)`; `(B)` goes back from the slot list.
- [ ] Walk with `(L stick)`, and run by holding `(B)`; the farmer plays the walk animation, the run animation while running, and the idle animation when he stops.
- [ ] Turn the camera all the way round with `(R stick)` left and right, and zoom to the nearest and farthest limits with `(R stick)` up and down; zoomed out looks steeply down, zoomed in looks lower. The prompts keep showing controller buttons.
- [ ] With the camera turned to face east, south, west and a diagonal, push forward: the farmer walks away from the camera. Stop: the farmer faces the outlined tile, the outline is visible even behind the farmer when zoomed in, and the Use prompt names what Use does to that tile; press use and it does that.
- [ ] Cycle the held item with `(LB)` and `(RB)`; the hotbar hint reads `(LB) (RB) Change item`.
- [ ] Cycle the hotbar row with `(View)`; the hotbar shows slots 7–12, then 1–6 again; the hint reads `(View) Next row` and the row label `Row 2/2`, then `Row 1/2`.
- [ ] Till a Tile with the Hoe, plant a seed and water it, each with `(A)`; the prompt reads `(A) Till`, `(A) Plant …` and `(A) Water`; the farmer plays the matching animation for each (the hoe swing for till, then the plant and water animations), and a rejected Use (watering with an empty can) plays no animation.
- [ ] Refill the can at the pond with `(X)`; the hint reads `(X) Refill can`; the farmer plays the refill animation.
- [ ] Harvest a ripe crop with `(A)`; the prompt reads `(A) Harvest …`; the farmer plays the harvest animation.
- [ ] Ship the crops at the bin with `(X)`; the hint reads `(X) Ship crops`; the farmer plays the ship animation.
- [ ] Open the shop with `(X)` (the hint reads `(X) Shop`); the Close button reads `(B) Close`.
- [ ] In the shop, buy a seed with the D-pad and `(A)`; the gold goes down and the seed count goes up.
- [ ] In the shop, after the focus is lost, a D-pad press gives the first button focus again.
- [ ] Close the shop with `(B)`; play and the clock resume.
- [ ] Open the backpack with `(Y)`, move the hoe into the second row with the D-pad and `(A)`, and close it with `(B)`; the hotbar no longer shows the hoe on row 1, and the clock didn't move while the menu was open. While holding a slot, `(B)` puts it back without closing.
- [ ] Pause with `(Start)` (the panel reads `(Start) Resume`), then resume with `(Start)`.
- [ ] Sleep at the door with `(X)`; the hint reads `(X) Sleep`; the farmer turns to the door and plays the sleep animation (a yawn and a stretch) before the night fade.
- [ ] The night fade plays and the next morning starts at 6:00 am, with the shipped crops paid.
- [ ] On another Day, let the clock reach 2:00 am: the farmer passes out (the pass out animation, ending on the ground), then the night fade plays and "You passed out…" shows; he wakes standing at the door playing the idle animation and can move and use tools at once.
- [ ] Walk off the farm through the gap in the middle of its south edge with `(L stick)`: the forest path loads with the farmer at its north end facing south. Walk down the winding path (the trees block only at their trunks) and off its south end into town, through the square past the store, the carpenter and the blacksmith and off the town's south edge to the lakeshore; the shore stops him at the water and he can walk out on the dock. Walk back the same way: each exit brings him in beside the exit he just used, in the next area facing into it, and back on the farm the tilled, planted and watered Tiles, the backpack, gold, energy, can and clock are as he left them.
- [ ] Empty the can, walk along the river on the farm's east edge from its north end to its south end, and refill the can there with `(X)`: the hint reads `(X) Refill can` all the way along the bank and goes when he steps away; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk to the river on the town's east side (either side of the broken bridge stub) and refill the can with `(X)`; the hint reads `(X) Refill can`; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk out to the end of the lakeshore's dock and refill the can at the lake with `(X)`; the can is full. Then refill it at the lake from the shore beside the dock the same way.
- [ ] In town, pause with `(Start)` for 30 seconds: the clock does not move. Resume, then let the clock reach 2:00 am in town: he passes out, "You passed out…" shows, and the next morning starts on the farm with him at the door.

## Keyboard-and-mouse-only pass

Buttons: move `[WASD]` (or the arrow keys), run `[Shift]`, use `[Space]` (or left click), interact `[F]` (or right
click), previous item `[Q]` (or wheel up), next item `[E]` (or wheel down), next hotbar row `[X]`, open the backpack `[Tab]`, pause `[Esc]`, close a menu `[Esc]`,
turn the camera left `[Z]` and right `[C]`, zoom the camera in `[R]` and out `[V]`; menus move with the arrow keys and
press with `[Enter]`, or are clicked with the mouse.

- [ ] Before you begin, the mouse cursor is visible, and stays visible for the whole pass.
- [ ] Title screen: start a new game, or load a saved game, with the arrow keys and `[Enter]` or a mouse click; `[Esc]` goes back from the slot list.
- [ ] Walk with `[WASD]`, and run by holding `[Shift]`; the farmer plays the walk animation, the run animation while running, and the idle animation when he stops.
- [ ] Turn the camera all the way round with `[Z]` (left) and `[C]` (right), and zoom to the nearest and farthest limits with `[R]` (in) and `[V]` (out); zoomed out looks steeply down, zoomed in looks lower. The prompts keep showing keyboard keys.
- [ ] With the camera turned to face east, south, west and a diagonal, push forward: the farmer walks away from the camera. Stop: the farmer faces the outlined tile, the outline is visible even behind the farmer when zoomed in, and the Use prompt names what Use does to that tile; press use and it does that.
- [ ] Cycle the held item with `[Q]` and `[E]` and with the mouse wheel; the hotbar hint reads `[Q] [E] Change item`.
- [ ] Cycle the hotbar row with `[X]`; the hotbar shows slots 7–12, then 1–6 again; the hint reads `[X] Next row` and the row label `Row 2/2`, then `Row 1/2`.
- [ ] Till a Tile with the Hoe, plant a seed and water it, each with `[Space]` or left click; the prompt reads `[Space] Till`, `[Space] Plant …` and `[Space] Water`; the farmer plays the matching animation for each (the hoe swing for till, then the plant and water animations), and a rejected Use (watering with an empty can) plays no animation.
- [ ] Refill the can at the pond with `[F]`; the hint reads `[F] Refill can`; the farmer plays the refill animation.
- [ ] Harvest a ripe crop with `[Space]`; the prompt reads `[Space] Harvest …`; the farmer plays the harvest animation.
- [ ] Ship the crops at the bin with `[F]`; the hint reads `[F] Ship crops`; the farmer plays the ship animation.
- [ ] Open the shop with `[F]` (the hint reads `[F] Shop`); the Close button reads `[Esc] Close`.
- [ ] In the shop, buy a seed with the arrow keys and `[Enter]`, and another with a mouse click; the gold goes down and the seed count goes up.
- [ ] In the shop, click empty panel space so nothing has focus, then press an arrow key: the first button has focus again.
- [ ] Close the shop with `[Esc]`; play and the clock resume.
- [ ] Open the backpack with `[Tab]`, move the hoe into the second row with the arrow keys and `[Space]` (or two mouse clicks), and close it with `[Esc]`; the hotbar no longer shows the hoe on row 1, and the clock didn't move while the menu was open. While holding a slot, `[Esc]` puts it back without closing.
- [ ] Pause with `[Esc]` (the panel reads `[Esc] Resume`), then resume with `[Esc]`.
- [ ] Sleep at the door with `[F]`; the hint reads `[F] Sleep`; the farmer turns to the door and plays the sleep animation (a yawn and a stretch) before the night fade.
- [ ] The night fade plays and the next morning starts at 6:00 am, with the shipped crops paid.
- [ ] On another Day, let the clock reach 2:00 am: the farmer passes out (the pass out animation, ending on the ground), then the night fade plays and "You passed out…" shows; he wakes standing at the door playing the idle animation and can move and use tools at once.
- [ ] Walk off the farm through the gap in the middle of its south edge with `[WASD]`: the forest path loads with the farmer at its north end facing south. Walk down the winding path (the trees block only at their trunks) and off its south end into town, through the square past the store, the carpenter and the blacksmith and off the town's south edge to the lakeshore; the shore stops him at the water and he can walk out on the dock. Walk back the same way: each exit brings him in beside the exit he just used, in the next area facing into it, and back on the farm the tilled, planted and watered Tiles, the backpack, gold, energy, can and clock are as he left them.
- [ ] Empty the can, walk along the river on the farm's east edge from its north end to its south end, and refill the can there with `[F]`: the hint reads `[F] Refill can` all the way along the bank and goes when he steps away; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk to the river on the town's east side (either side of the broken bridge stub) and refill the can with `[F]`; the hint reads `[F] Refill can`; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk out to the end of the lakeshore's dock and refill the can at the lake with `[F]`; the can is full. Then refill it at the lake from the shore beside the dock the same way.
- [ ] In town, pause with `[Esc]` for 30 seconds: the clock does not move. Resume, then let the clock reach 2:00 am in town: he passes out, "You passed out…" shows, and the next morning starts on the farm with him at the door.

## Steam Deck (Game Mode) pass

Run the release build from Steam in Game Mode, with the Deck's built-in controls only (Steam Input's default gamepad
layout, so the buttons match the controller glyphs: `(A)`, `(B)`, `(X)`, `(Y)`, `(LB)` is L1, `(RB)` is R1, `(View)` is
the ⧉ button, `(Start)` is the ☰ button). The right stick turns the camera left and right and zooms it in (up) and out (down). Frame rate and
text size are checked in epic #12's Steam Deck pass, not here; the frame-rate reading at the farthest zoom is
requested on issue #71's pull request.

- [ ] Once a Deck control is pressed, no mouse cursor shows in the middle of the screen.
- [ ] Title screen: start a new game, or load a saved game, with the D-pad and `(A)`.
- [ ] Walk with the left stick, and run by holding `(B)`; the farmer plays the walk animation, the run animation while running, and the idle animation when he stops.
- [ ] Turn the camera all the way round and zoom it to the nearest and farthest limits with the right stick; the prompts keep showing controller glyphs. With the camera turned, push the left stick forward: the farmer walks away from the camera. Stop: the farmer faces the outlined tile, and the outline is visible even behind the farmer when zoomed in.
- [ ] Cycle the held item with L1 and R1; the hotbar hint reads `(LB) (RB) Change item`.
- [ ] Cycle the hotbar row with `(View)` (the ⧉ button); the hotbar shows slots 7–12, then 1–6 again; the hint reads `(View) Next row`.
- [ ] Till a Tile, plant a seed and water it, each with `(A)`; every prompt shows a controller glyph; the farmer plays the matching animation for each (the hoe swing for till, then the plant and water animations), and a rejected Use (watering with an empty can) plays no animation.
- [ ] Refill the can at the pond with `(X)`; the farmer plays the refill animation.
- [ ] Harvest a ripe crop with `(A)`; the farmer plays the harvest animation.
- [ ] Ship the crops at the bin with `(X)`; the farmer plays the ship animation.
- [ ] Open the shop with `(X)`, buy a seed with the D-pad and `(A)`, and close the shop with `(B)`; the Close button reads `(B) Close`.
- [ ] Touch the touchscreen inside the shop, then press the D-pad: the first button has focus again.
- [ ] Open the backpack with `(Y)`, move the hoe into the second row with the D-pad and `(A)`, and close it with `(B)`; the hotbar no longer shows the hoe on row 1, and the clock didn't move while the menu was open.
- [ ] Pause with ☰ (`(Start)`), then resume with ☰.
- [ ] Sleep at the door with `(X)`; the farmer turns to the door and plays the sleep animation (a yawn and a stretch) before the night fade.
- [ ] The night fade plays and the next morning starts at 6:00 am.
- [ ] On another Day, let the clock reach 2:00 am: the farmer passes out (the pass out animation, ending on the ground), then the night fade plays and "You passed out…" shows; he wakes standing at the door playing the idle animation and can move and use tools at once.
- [ ] Walk off the farm through the gap in the middle of its south edge with the left stick: the forest path loads with the farmer at its north end facing south. Walk down the winding path (the trees block only at their trunks) and off its south end into town, through the square past the store, the carpenter and the blacksmith and off the town's south edge to the lakeshore; the shore stops him at the water and he can walk out on the dock. Walk back the same way: each exit brings him in beside the exit he just used, in the next area facing into it, and back on the farm the tilled, planted and watered Tiles, the backpack, gold, energy, can and clock are as he left them.
- [ ] Empty the can, walk along the river on the farm's east edge from its north end to its south end, and refill the can there with `(X)`: the Refill can hint shows all the way along the bank and goes when he steps away; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk to the river on the town's east side (either side of the broken bridge stub) and refill the can with `(X)`; the Refill can hint shows; the can is full and the farmer plays the refill animation.
- [ ] Empty the can, walk out to the end of the lakeshore's dock and refill the can at the lake with `(X)`; the can is full. Then refill it at the lake from the shore beside the dock the same way.
- [ ] In town, pause with ☰ (`(Start)`) for 30 seconds: the clock does not move. Resume, then let the clock reach 2:00 am in town: he passes out, "You passed out…" shows, and the next morning starts on the farm with him at the door.
- [ ] Started with `--show-fps` (1280 x 800), walk the full length of each area: the farm, the forest path, the town and the lakeshore each hold 30 fps the whole way (REQ-14). Write the lowest reading for each area here: farm ___, forest path ___, town ___, lakeshore ___.
