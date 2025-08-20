import numpy as np
import pandas as pd
from scipy.stats import chi2


def compute_norm(df: pd.DataFrame) -> np.float64:
    """
    Compute the Euclidean distance between two points.

    Parameters
    ----------
    df : pd.DataFrame
        DataFrame containing the coordinates of the points.

    Returns
    -------
    float
        Euclidean distance between the two points.
    """
    return np.sqrt((df**2).sum())


def central_derivative(df: pd.DataFrame, time: pd.Series, window: int) -> pd.DataFrame:
    """
    Compute the central finite difference derivative of a DataFrame
    with respect to a time Series.

    Parameters
    ----------
    df : pd.DataFrame
        Values to differentiate.
    time : pd.Series
        Time values aligned with df.index.
    window : int
        The number of points to use for the central difference.

    Returns
    -------
    pd.DataFrame
        Approximate derivatives of df with respect to time.
    """
    numerator = df.shift(-window) - df.shift(window)
    denominator = time.diff(2).shift(-window)
    return numerator.div(denominator, axis=0)


def fit_confidence_ellipse(data: pd.DataFrame, confidence: float = 0.95) -> pd.DataFrame:
    """
    Fit a confidence ellipse to 2D data using PCA.

    Parameters
    ----------
    data : pd.DataFrame
        DataFrame containing the x and y coordinates of the points.
    confidence : float
        Confidence level for the ellipse (default is 0.95).

    Returns
    -------
    pd.DataFrame
        DataFrame containing the parameters of the fitted ellipse:
        - center_x: x-coordinate of the ellipse center
        - center_y: y-coordinate of the ellipse center
        - a: semi-major axis length
        - b: semi-minor axis length
        - theta: rotation angle of the ellipse in radians
    """
    if data.shape[1] != 2:
        raise ValueError("Data must contain exactly two columns for x and y coordinates.")

    # Compute mean (ellipse center)
    center = data.mean().values

    # Compute the covariance matrix
    cov = data.cov().values
    eigenvalues, eigenvectors = np.linalg.eigh(cov)
    sorted_indices = np.argsort(eigenvalues)[::-1]
    eigenvalues = eigenvalues[sorted_indices]
    eigenvectors = eigenvectors[:, sorted_indices]

    # Confidence scaling factor
    k = np.sqrt(chi2.ppf(confidence, df=2))

    # Compute the semi-major and semi-minor axes
    a = np.sqrt(eigenvalues[0]) * k
    b = np.sqrt(eigenvalues[1]) * k

    # Compute the angle of rotation
    theta = np.arctan2(eigenvectors[1, 0], eigenvectors[0, 0])

    # Return the parameters of the fitted ellipse
    return pd.DataFrame({"center_x": [center[0]], "center_y": [center[1]], "a": [a], "b": [b], "theta": [theta]})
