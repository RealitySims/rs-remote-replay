mkdir tmp

move ".\Assets\RealitySims\Remote Replay\Examples\IndieMarc.meta" tmp\
move ".\Assets\RealitySims\Remote Replay\Examples\JumpUp.meta" tmp\
move ".\Assets\RealitySims\Remote Replay\Examples\Tanks.meta" tmp\

move ".\Assets\RealitySims\Remote Replay\Examples\IndieMarc" tmp\
move ".\Assets\RealitySims\Remote Replay\Examples\JumpUp" tmp\
move ".\Assets\RealitySims\Remote Replay\Examples\Tanks" tmp\

"C:\Program Files\Unity\Hub\Editor\2021.3.21f1\Editor\Unity.exe" -quit -batchmode -nographics -projectPath "C:\Users\Pavel\rs-remote-replay" -exportPackage "Assets\RealitySims" "C:\Users\Pavel\rs-remote-replay\UnityPackage\RemoteReplay.unitypackage"

move "tmp\*" ".\Assets\RealitySims\Remote Replay\Examples\"
move "tmp\IndieMarc" ".\Assets\RealitySims\Remote Replay\Examples\"
move "tmp\JumpUp" ".\Assets\RealitySims\Remote Replay\Examples\"
move "tmp\Tanks" ".\Assets\RealitySims\Remote Replay\Examples\"

rmdir tmp