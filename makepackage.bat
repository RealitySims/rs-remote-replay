mkdir Assets\Build
robocopy Assets\RealitySims\ Assets\Build\RealitySims\ /mir
rmdir .\Assets\Build\RealitySims\Examples\IndieMarc\
rmdir .\Assets\Build\RealitySims\Examples\JumpUp\
rmdir .\Assets\Build\RealitySims\Examples\Tanks\
del .\Assets\Build\RealitySims\Examples\IndieMarc.meta
del .\Assets\Build\RealitySims\Examples\JumpUp.meta
del .\Assets\Build\RealitySims\Examples\Tanks.meta
"C:\Program Files\Unity\Hub\Editor\2021.3.21f1\Editor\Unity.exe" -quit -batchmode -nographics -projectPath "C:\Users\Pavel\rs-remote-replay" -exportPackage "Assets\Build" "C:\Users\Pavel\rs-remote-replay\UnityPackage\RemoteReplay.unitypackage"
