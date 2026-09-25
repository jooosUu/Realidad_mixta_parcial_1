import subprocess, os

env = os.environ.copy()
env['GIT_AUTHOR_NAME'] = 'jooosUu'
env['GIT_AUTHOR_EMAIL'] = '141538169+jooosUu@users.noreply.github.com'
env['GIT_COMMITTER_NAME'] = 'jooosUu'
env['GIT_COMMITTER_EMAIL'] = '141538169+jooosUu@users.noreply.github.com'

def run_git(args, env_override=None, input_data=None):
    e = env.copy()
    if env_override:
        e.update(env_override)
    p = subprocess.Popen(['git'] + args, stdin=subprocess.PIPE if input_data else None, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, env=e)
    out, err = p.communicate(input=input_data)
    if p.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)} failed: {err}")
    return out.strip()

index_file = 'temp_index.bin'
if os.path.exists(index_file):
    os.remove(index_file)

e_idx = env.copy()
e_idx['GIT_INDEX_FILE'] = index_file

# Read b59e77c into index
run_git(['read-tree', 'b59e77c'], env_override={'GIT_INDEX_FILE': index_file})

# Get all files in index
ls_out = run_git(['ls-files', '-s'], env_override={'GIT_INDEX_FILE': index_file}).splitlines()

c1_prefixes = ('Packages/', 'ProjectSettings/', 'Assets/Plugins/', 'Assets/XR/', 'Assets/Shaders/', 'Assets/Scripts/Core/', '.gitignore')
c2_prefixes = ('Assets/Scripts/Gaze/', 'Assets/Scripts/Museum/', 'Assets/Generated/Textures/')
c3_prefixes = ('Assets/Editor/Kit.cs', 'Assets/Editor/ModelFactory.cs', 'Assets/Editor/ModelLibrary.cs', 'Assets/Editor/Screenshots.cs', 'Blender/', 'Assets/Models/')

# Filter for Tree 1
run_git(['read-tree', '--empty'], env_override={'GIT_INDEX_FILE': index_file})
lines_c1 = [l for l in ls_out if any(l.split('\t')[1].startswith(p) for p in c1_prefixes)]
run_git(['update-index', '--index-info'], env_override={'GIT_INDEX_FILE': index_file}, input_data='\n'.join(lines_c1) + '\n')
tree1 = run_git(['write-tree'], env_override={'GIT_INDEX_FILE': index_file})

# Filter for Tree 2
lines_c2 = [l for l in ls_out if any(l.split('\t')[1].startswith(p) for p in c1_prefixes + c2_prefixes)]
run_git(['read-tree', '--empty'], env_override={'GIT_INDEX_FILE': index_file})
run_git(['update-index', '--index-info'], env_override={'GIT_INDEX_FILE': index_file}, input_data='\n'.join(lines_c2) + '\n')
tree2 = run_git(['write-tree'], env_override={'GIT_INDEX_FILE': index_file})

# Filter for Tree 3
lines_c3 = [l for l in ls_out if any(l.split('\t')[1].startswith(p) for p in c1_prefixes + c2_prefixes + c3_prefixes)]
run_git(['read-tree', '--empty'], env_override={'GIT_INDEX_FILE': index_file})
run_git(['update-index', '--index-info'], env_override={'GIT_INDEX_FILE': index_file}, input_data='\n'.join(lines_c3) + '\n')
tree3 = run_git(['write-tree'], env_override={'GIT_INDEX_FILE': index_file})

# Tree 4 is 9675629 (Friday square vestibule)
tree4 = run_git(['rev-parse', '9675629^{tree}'])

# Tree 5 is d256461 (Redistribucion pasillo)
tree5 = run_git(['rev-parse', 'd256461^{tree}'])

# Tree 6 is a1d1509 (Reproductor + contemplar + luces)
tree6 = run_git(['rev-parse', 'a1d1509^{tree}'])

# Tree 7 is 2ae62be (Tickets + joystick + camara)
tree7 = run_git(['rev-parse', '2ae62be^{tree}'])

# Tree 8 is current working directory / staged with new README.md
run_git(['read-tree', 'dc7db09'], env_override={'GIT_INDEX_FILE': index_file})
readme_blob = run_git(['hash-object', '-w', 'README.md'])
readme_mode = '100644'
run_git(['update-index', '--add', '--cacheinfo', f'{readme_mode},{readme_blob},README.md'], env_override={'GIT_INDEX_FILE': index_file})
tree8 = run_git(['write-tree'], env_override={'GIT_INDEX_FILE': index_file})

if os.path.exists(index_file):
    os.remove(index_file)

print('Trees successfully built:')
print('Tree 1:', tree1)
print('Tree 2:', tree2)
print('Tree 3:', tree3)
print('Tree 4:', tree4)
print('Tree 5:', tree5)
print('Tree 6:', tree6)
print('Tree 7:', tree7)
print('Tree 8:', tree8)
