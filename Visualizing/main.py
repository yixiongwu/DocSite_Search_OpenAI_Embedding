import matplotlib
import matplotlib.pyplot as plt
import pandas as pd
from sklearn.manifold import TSNE
import numpy as np

# Load the embeddings
# If you have not run the "Obtain_dataset.ipynb" notebook, you can download the datafile from here: https://cdn.openai.com/API/examples/data/fine_food_reviews_with_embeddings_1k.csv
# datafile_path = "./fine_food_reviews_with_embeddings_1k.csv"
datafile_path = "./docItems.csv"
df = pd.read_csv(datafile_path)

# Convert to a list of lists of floats
matrix = np.array(df.Embedding.apply(eval).to_list())

# Create a t-SNE model and transform the data
tsne = TSNE(n_components=2, perplexity=15, random_state=42,
            init='random', learning_rate=200)
vis_dims = tsne.fit_transform(matrix)
vis_dims.shape

df['Score'] = pd.factorize(df['Category'])[0]

categoies = ["avatar",
             "building-and-visuals",
             "education", "getting-started",
             "mechanics",
             "open-cloud",
             "optimization",
             "production",
             "resources",
             "scripting",
             "studio",
             "tools",
             "tutorials"]
colors = ["red",
          "darkorange",
          "gold",
          "turquoise",
          "darkgreen",
          "blue",
          "gray",
          "black",
          "aqua",
          "pink",
          "green",
          "yellow",
          "plum"]
x = [x for x, y in vis_dims]
y = [y for x, y in vis_dims]
color_indices = df.Score.values - 1

colormap = matplotlib.colors.ListedColormap(colors)
plt.scatter(x, y, c=color_indices, cmap=colormap, alpha=0.3)
for score in [0, 1, 2, 3, 4]:
    avg_x = np.array(x)[df.Score-1 == score].mean()
    avg_y = np.array(y)[df.Score-1 == score].mean()
    color = colors[score]
    plt.scatter(avg_x, avg_y, marker='x', color=color, s=100)

plt.title("Roblox doc site articles")
plt.show()
