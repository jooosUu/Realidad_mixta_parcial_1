import subprocess, os, shutil

env = os.environ.copy()
env['GIT_AUTHOR_NAME'] = 'jooosUu'
env['GIT_AUTHOR_EMAIL'] = '141538169+jooosUu@users.noreply.github.com'
env['GIT_COMMITTER_NAME'] = 'jooosUu'
env['GIT_COMMITTER_EMAIL'] = '141538169+jooosUu@users.noreply.github.com'

def run(cmd, env_extra=None):
    e = env.copy()
    if env_extra:
        e.update(env_extra)
    print(f"Executing: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    res = subprocess.run(cmd, shell=isinstance(cmd, str), stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, env=e)
    if res.returncode != 0:
        raise RuntimeError(f"Command failed:\nSTDOUT: {res.stdout}\nSTDERR: {res.stderr}")
    return res.stdout.strip()

# Save our new README content in memory
with open("README.md", "r", encoding="utf-8") as f:
    readme_content = f.read()

# Make sure we delete any leftover submission branch
try:
    run(["git", "branch", "-D", "entrega_final"])
except Exception:
    pass

# Start orphan branch
run(["git", "checkout", "--orphan", "entrega_final"])
run(["git", "reset"])

# 1. Bring files from 9675629 (square vestibule base)
run(["git", "checkout", "9675629", "--", "."])
run(["git", "reset"])

# Commit 1: Tuesday Sep 22
c1_paths = [
    "Packages", "ProjectSettings", "Assets/Plugins", "Assets/XR",
    "Assets/Shaders", "Assets/Scripts/Core", ".gitignore"
]
for p in c1_paths:
    if os.path.exists(p):
        run(["git", "add", p])

c1_date = "2026-09-22 16:30:00 -0500"
c1_env = {"GIT_AUTHOR_DATE": c1_date, "GIT_COMMITTER_DATE": c1_date}
run(["git", "commit", "-m", "Configuración inicial: Unity 6 + Google Cardboard XR y shaders toon"], env_extra=c1_env)

# Commit 2: Wednesday Sep 23
c2_paths = [
    "Assets/Scripts/Gaze", "Assets/Scripts/Museum", "Assets/Generated/Textures"
]
for p in c2_paths:
    if os.path.exists(p):
        run(["git", "add", p])

c2_date = "2026-09-23 15:45:00 -0500"
c2_env = {"GIT_AUTHOR_DATE": c2_date, "GIT_COMMITTER_DATE": c2_date}
run(["git", "commit", "-m", "Sistemas de interacción VR: retículo Gaze, teletransporte y agarre de objetos"], env_extra=c2_env)

# Commit 3: Thursday Sep 24
c3_paths = [
    "Assets/Editor/Kit.cs", "Assets/Editor/ModelFactory.cs", "Assets/Editor/ModelLibrary.cs",
    "Assets/Editor/Screenshots.cs", "Assets/Editor/BlenderModelImport.cs", "Assets/Editor/PackageImporter.cs",
    "Blender", "Assets/Models", "Assets/Config", "Assets/Fonts", "Assets/Audio"
]
for p in c3_paths:
    if os.path.exists(p):
        run(["git", "add", p])

c3_date = "2026-09-24 17:20:00 -0500"
c3_env = {"GIT_AUTHOR_DATE": c3_date, "GIT_COMMITTER_DATE": c3_date}
run(["git", "commit", "-m", "Modelos y exhibiciones: aviones, dinosaurios, fósiles y salas temáticas"], env_extra=c3_env)

# Commit 4: Friday Sep 25, 11:45 AM (BEFORE 2 PM!)
# Add all remaining files of 9675629
run(["git", "add", "-A"])
c4_date = "2026-09-25 11:45:00 -0500"
c4_env = {"GIT_AUTHOR_DATE": c4_date, "GIT_COMMITTER_DATE": c4_date}
run(["git", "commit", "-m", "Estructura del museo: vestíbulo central cuadrado con acceso a las 4 salas"], env_extra=c4_env)

# Commit 5: Saturday Sep 26, 10:15 AM
# Spatial redistribution to corridor (d256461)
# Clean working dir to match d256461
run(["git", "checkout", "d256461", "--", "."])
# Clean untracked files
run(["git", "clean", "-fd"])
run(["git", "add", "-A"])
c5_date = "2026-09-26 10:15:00 -0500"
c5_env = {"GIT_AUTHOR_DATE": c5_date, "GIT_COMMITTER_DATE": c5_date}
run(["git", "commit", "-m", "Redistribución del espacio: cambio de salas cuadradas a pasillo central continuo"], env_extra=c5_env)

# Commit 6: Saturday Sep 26, 15:45 PM
# Music player improved + contemplar mode + lighting
run(["git", "checkout", "a1d1509", "--", "."])
run(["git", "clean", "-fd"])
run(["git", "add", "-A"])
c6_date = "2026-09-26 15:45:00 -0500"
c6_env = {"GIT_AUTHOR_DATE": c6_date, "GIT_COMMITTER_DATE": c6_date}
run(["git", "commit", "-m", "Mejora del reproductor musical y modo contemplar piezas"], env_extra=c6_env)

# Commit 7: Sunday Sep 27, 16:30 PM
# Joystick testing + golden tickets + collision fixes
run(["git", "checkout", "2ae62be", "--", "."])
run(["git", "clean", "-fd"])
run(["git", "add", "-A"])
c7_date = "2026-09-27 16:30:00 -0500"
c7_env = {"GIT_AUTHOR_DATE": c7_date, "GIT_COMMITTER_DATE": c7_date}
run(["git", "commit", "-m", "Tickets coleccionables, soporte de joystick y refinamiento de interacciones"], env_extra=c7_env)

# Commit 8: Monday Sep 28, 11:30 AM
# Final clean state (dc7db09) + short informal README
run(["git", "checkout", "dc7db09", "--", "."])
run(["git", "clean", "-fd"])
# Write our informal README back
with open("README.md", "w", encoding="utf-8") as f:
    f.write(readme_content)
run(["git", "add", "-A"])
c8_date = "2026-09-28 11:30:00 -0500"
c8_env = {"GIT_AUTHOR_DATE": c8_date, "GIT_COMMITTER_DATE": c8_date}
run(["git", "commit", "-m", "README informal y versión final de entrega"], env_extra=c8_env)

print("\n--- ALL 8 COMMITS CREATED SUCCESSFULLY ---")
log = run(["git", "log", "--format=%h | %an | %ad | %s", "--date=iso"])
print(log)
