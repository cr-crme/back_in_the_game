from enum import Enum

import numpy as np
import pandas as pd

from .data import Data, DataTag, DataAxis, DataType
from .maths import fit_confidence_ellipse, compute_norm


def _first_where(series: pd.Series) -> int:
    return series.idxmax().iloc[0]


class DataMetrics(Enum):
    OVERALL_HEAD_HORIZONTAL_DISPERSION = "Head dispersion"
    OVERALL_LEFT_HAND_ACCELERATION_PEAK = "Left hand acceleration peak"
    OVERALL_RIGHT_HAND_ACCELERATION_PEAK = "Right hand acceleration peak"
    OVERALL_LEFT_HAND_TRAVELED_DISTANCE = "Left hand traveled distance"
    OVERALL_RIGHT_HAND_TRAVELED_DISTANCE = "Right hand traveled distance"
    SQUAT_HEIGHT = "Squat height"
    JUMP_HEIGHT = "Jump height"
    JUMP_DISTANCE = "Jump distance"
    JUMP_FLIGHT_TIME = "Jump flight time"
    PRE_JUMP_HEAD_HORIZONTAL_DISPERSION = "Pre-jump head horizontal dispersion"
    PRE_JUMP_LEFT_HAND_ACCELERATION_PEAK = "Pre-jump left hand acceleration peak"
    PRE_JUMP_RIGHT_HAND_ACCELERATION_PEAK = "Pre-jump right hand acceleration peak"
    PRE_JUMP_LEFT_HAND_TRAVELED_DISTANCE = "Pre-jump left hand traveled distance"
    PRE_JUMP_RIGHT_HAND_TRAVELED_DISTANCE = "Pre-jump right hand traveled distance"
    POST_JUMP_HEAD_HORIZONTAL_DISPERSION = "Post-jump head horizontal dispersion"
    POST_JUMP_LEFT_HAND_ACCELERATION_PEAK = "Post-jump left hand acceleration peak"
    POST_JUMP_RIGHT_HAND_ACCELERATION_PEAK = "Post-jump right hand acceleration peak"
    POST_JUMP_LEFT_HAND_TRAVELED_DISTANCE = "Post-jump left hand traveled distance"
    POST_JUMP_RIGHT_HAND_TRAVELED_DISTANCE = "Post-jump right hand traveled distance"
    JUMP_INDICES = "Jump indices"


class DataIndicesExtraction(Enum):
    SQUAT_START = "squat_start"
    SQUAT_DEEPEST = "squat_deepest"
    SQUAT_END = "squat_end"
    TOE_OFF = "toe_off"
    HIGHEST_POINT = "highest_point"
    RECEPTION = "reception"


def data_extraction(data: Data) -> dict[DataMetrics, float]:
    overall_head_horizontal_dispersion = _horizontal_dispersion(data, DataTag.HEAD_POSITION)
    overall_left_hand_acceleration_peak = _acceleration_peak(data, DataTag.LEFT_HAND_POSITION)
    overall_right_hand_acceleration_peak = _acceleration_peak(data, DataTag.RIGHT_HAND_POSITION)
    overall_left_hand_traveled_distance = _traveled_distance(data, DataTag.LEFT_HAND_POSITION)
    overall_right_hand_traveled_distance = _traveled_distance(data, DataTag.RIGHT_HAND_POSITION)

    jump_indices = _jump_indices_extraction(data)
    squat_height = _squat_height(data, jump_indices)
    jump_height = _jump_height(data, jump_indices)
    jump_distance = _jump_distance(data, jump_indices)
    jump_time = _flight_time(data, jump_indices)

    if jump_indices[DataIndicesExtraction.TOE_OFF] is None:
        pre_jump_head_horizontal_dispersion = np.nan
        pre_jump_left_hand_acceleration_peak = np.nan
        pre_jump_right_hand_acceleration_peak = np.nan
        pre_jump_left_hand_traveled_distance = np.nan
        pre_jump_right_hand_traveled_distance = np.nan
    else:
        t = slice(None, jump_indices[DataIndicesExtraction.TOE_OFF])
        pre_jump_head_horizontal_dispersion = _horizontal_dispersion(data, DataTag.HEAD_POSITION, t=t)
        pre_jump_left_hand_acceleration_peak = _acceleration_peak(data, DataTag.LEFT_HAND_POSITION, t=t)
        pre_jump_right_hand_acceleration_peak = _acceleration_peak(data, DataTag.RIGHT_HAND_POSITION, t=t)
        pre_jump_left_hand_traveled_distance = _traveled_distance(data, DataTag.LEFT_HAND_POSITION, t=t)
        pre_jump_right_hand_traveled_distance = _traveled_distance(data, DataTag.RIGHT_HAND_POSITION, t=t)

    if jump_indices[DataIndicesExtraction.RECEPTION] is None:
        post_jump_head_horizontal_dispersion = np.nan
        post_jump_left_hand_acceleration_peak = np.nan
        post_jump_right_hand_acceleration_peak = np.nan
        post_jump_left_hand_traveled_distance = np.nan
        post_jump_right_hand_traveled_distance = np.nan
    else:
        t = slice(jump_indices[DataIndicesExtraction.RECEPTION], None)
        post_jump_head_horizontal_dispersion = _horizontal_dispersion(data, DataTag.HEAD_POSITION, t=t)
        post_jump_left_hand_acceleration_peak = _acceleration_peak(data, DataTag.LEFT_HAND_POSITION, t=t)
        post_jump_right_hand_acceleration_peak = _acceleration_peak(data, DataTag.RIGHT_HAND_POSITION, t=t)
        post_jump_left_hand_traveled_distance = _traveled_distance(data, DataTag.LEFT_HAND_POSITION, t=t)
        post_jump_right_hand_traveled_distance = _traveled_distance(data, DataTag.RIGHT_HAND_POSITION, t=t)

    return {
        DataMetrics.OVERALL_HEAD_HORIZONTAL_DISPERSION: overall_head_horizontal_dispersion,
        DataMetrics.OVERALL_LEFT_HAND_ACCELERATION_PEAK: overall_left_hand_acceleration_peak,
        DataMetrics.OVERALL_RIGHT_HAND_ACCELERATION_PEAK: overall_right_hand_acceleration_peak,
        DataMetrics.OVERALL_LEFT_HAND_TRAVELED_DISTANCE: overall_left_hand_traveled_distance,
        DataMetrics.OVERALL_RIGHT_HAND_TRAVELED_DISTANCE: overall_right_hand_traveled_distance,
        DataMetrics.SQUAT_HEIGHT: squat_height,
        DataMetrics.JUMP_HEIGHT: jump_height,
        DataMetrics.JUMP_DISTANCE: jump_distance,
        DataMetrics.JUMP_FLIGHT_TIME: jump_time,
        DataMetrics.PRE_JUMP_HEAD_HORIZONTAL_DISPERSION: pre_jump_head_horizontal_dispersion,
        DataMetrics.PRE_JUMP_LEFT_HAND_ACCELERATION_PEAK: pre_jump_left_hand_acceleration_peak,
        DataMetrics.PRE_JUMP_RIGHT_HAND_ACCELERATION_PEAK: pre_jump_right_hand_acceleration_peak,
        DataMetrics.PRE_JUMP_LEFT_HAND_TRAVELED_DISTANCE: pre_jump_left_hand_traveled_distance,
        DataMetrics.PRE_JUMP_RIGHT_HAND_TRAVELED_DISTANCE: pre_jump_right_hand_traveled_distance,
        DataMetrics.POST_JUMP_HEAD_HORIZONTAL_DISPERSION: post_jump_head_horizontal_dispersion,
        DataMetrics.POST_JUMP_LEFT_HAND_ACCELERATION_PEAK: post_jump_left_hand_acceleration_peak,
        DataMetrics.POST_JUMP_RIGHT_HAND_ACCELERATION_PEAK: post_jump_right_hand_acceleration_peak,
        DataMetrics.POST_JUMP_LEFT_HAND_TRAVELED_DISTANCE: post_jump_left_hand_traveled_distance,
        DataMetrics.POST_JUMP_RIGHT_HAND_TRAVELED_DISTANCE: post_jump_right_hand_traveled_distance,
        DataMetrics.JUMP_INDICES: jump_indices,
    }


def _squat_indices_extraction(data: Data) -> dict[DataIndicesExtraction, float]:
    #   1) Find the first time the velocity is more negative that a large threshold (-5m/s) (start_squat_idx_tp)
    #   2) From start_squat_idx_tp, walk backwards to find the first time the velocity threshold (-2m/s) (start_squat_idx)
    #   3) From start_squat_idx, walk forward to find the first time the velocity becomes positive (deepest_squat_idx)
    #   4) From deepest_squat_idx, walk forwards for the first time the velocity becomes negative again (end_squat_idx)

    try:
        velocity_safety_threshold = -5.0  # m/s
        velocity_threshold = -2.0  # m/s
        head_vel = data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.VERTICAL)

        start_squat_idx_tp = _first_where(head_vel < velocity_safety_threshold)
        start_squat_idx = _first_where(head_vel.loc[:start_squat_idx_tp].iloc[::-1] >= velocity_threshold)
        deepest_squat_idx = _first_where(head_vel.loc[start_squat_idx:] >= 0)
        end_squat_idx = _first_where(head_vel.loc[deepest_squat_idx:] < 0)

        if np.isnan(start_squat_idx) or np.isnan(deepest_squat_idx) or np.isnan(end_squat_idx) or start_squat_idx < 10:
            raise ValueError("Invalid squat indices")

        return {
            DataIndicesExtraction.SQUAT_START: start_squat_idx,
            DataIndicesExtraction.SQUAT_DEEPEST: deepest_squat_idx,
            DataIndicesExtraction.SQUAT_END: end_squat_idx,
        }
    except:
        return {
            DataIndicesExtraction.SQUAT_START: None,
            DataIndicesExtraction.SQUAT_DEEPEST: None,
            DataIndicesExtraction.SQUAT_END: None,
        }


def _jump_indices_extraction(data: Data) -> dict[DataIndicesExtraction, float]:
    # Jump specific metrics
    # Strategy:
    #   1) Find the squat data
    #   4) From deepest_squat_idx, walk forwards for the first time the acceleration becomes less than -9.81 (toe_off_idx)
    #   5) From toe_off_idx, find the first point the velocity becomes negative (highest_point_idx)
    #   6) From highest_point_idx, walk forwards for the first time the acceleration is larger than -9.81 (reception_idx)
    squat_data = _squat_indices_extraction(data)

    try:
        head_vel = data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.VERTICAL)
        head_acc = data.get(DataTag.HEAD_POSITION, data_type=DataType.ACCELERATION, axis=DataAxis.VERTICAL)
        gravity = -9.81  # m/s/s
        if squat_data[DataIndicesExtraction.SQUAT_DEEPEST] is None:
            raise ValueError("Invalid squat data")

        # Get useful aliases
        toe_off_idx = _first_where(head_acc.loc[squat_data[DataIndicesExtraction.SQUAT_DEEPEST] :] <= gravity)
        highest_point_idx = _first_where(head_vel.loc[toe_off_idx:] <= 0)
        reception_idx = _first_where(head_acc.loc[highest_point_idx:] >= gravity)

        if np.isnan(toe_off_idx) or np.isnan(highest_point_idx) or np.isnan(reception_idx):
            raise ValueError("Invalid jump indices")

        return {
            **squat_data,
            DataIndicesExtraction.TOE_OFF: toe_off_idx,
            DataIndicesExtraction.HIGHEST_POINT: highest_point_idx,
            DataIndicesExtraction.RECEPTION: reception_idx,
        }
    except:
        return {
            **squat_data,
            DataIndicesExtraction.TOE_OFF: None,
            DataIndicesExtraction.HIGHEST_POINT: None,
            DataIndicesExtraction.RECEPTION: None,
        }


def _horizontal_dispersion(data: Data, tag: DataTag, t: slice = slice(None)) -> np.float64:
    """Returns the horizontal dispersion (2D) of the specified tag."""
    position = data.get(tag=tag, t=t, data_type=DataType.VALUE, axis=DataAxis.HORIZONTAL)
    if position.empty:
        return np.nan

    # Return the area of the ellipse as a measure of dispersion (π * a * b)
    ellipse_params = fit_confidence_ellipse(position, confidence=0.95)
    return (ellipse_params["a"] * ellipse_params["b"] * np.pi).values[0]


def _acceleration_peak(data: Data, tag: DataTag, t: slice = slice(None), axis: DataAxis = DataAxis.ALL) -> np.float64:
    """Returns the peak acceleration of the specified tag."""
    acceleration = data.get(tag=tag, t=t, data_type=DataType.ACCELERATION, axis=axis)
    if acceleration.empty:
        return np.nan

    acceleration = compute_norm(acceleration)
    return acceleration.max()


def _traveled_distance(data: Data, tag: DataTag, t: slice = slice(None)) -> np.float64:
    """Returns the total traveled distance of the specified tag."""
    position = data.get(tag=tag, t=t, data_type=DataType.VALUE, axis=DataAxis.ALL)
    if position.empty:
        return np.nan

    diffs = position.diff().fillna(0)
    distances = compute_norm(diffs**2)
    return (distances.cumsum()).values[-1]


def _squat_height(data: Data, squat_indices: dict[DataIndicesExtraction, float]) -> np.float64:
    """
    Returns the squat height based on the head position.
    This is the vertical distance traveled from the squat start to the squat deepest point.
    """
    squat_deepest_idx = squat_indices[DataIndicesExtraction.SQUAT_DEEPEST]
    squat_start_idx = squat_indices[DataIndicesExtraction.SQUAT_START]
    if squat_start_idx is None or squat_deepest_idx is None:
        return np.nan

    head_vertical_position = data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.VERTICAL)
    if head_vertical_position.empty:
        return np.nan
    return compute_norm(head_vertical_position.iloc[squat_start_idx] - head_vertical_position.iloc[squat_deepest_idx])


def _jump_distance(data: Data, jump_indices: dict[DataIndicesExtraction, float]) -> np.float64:
    """
    Returns the horizontal jump distance based on the head position.
    This is the distance traveled horizontally from toe-off to the reception.
    """
    toe_off_idx = jump_indices[DataIndicesExtraction.TOE_OFF]
    reception_idx = jump_indices[DataIndicesExtraction.RECEPTION]
    if toe_off_idx is None or reception_idx is None:
        return np.nan

    head_horizontal_position = data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.HORIZONTAL)
    if head_horizontal_position.empty:
        return np.nan
    return compute_norm(head_horizontal_position.iloc[reception_idx] - head_horizontal_position.iloc[toe_off_idx])


def _jump_height(data: Data, jump_indices: dict[DataIndicesExtraction, float]) -> np.float64:
    """
    Returns the vertical jump height based on the head position.
    This is the vertical distance traveled from toe-off to the highest point.
    """
    highest_point_idx = jump_indices[DataIndicesExtraction.HIGHEST_POINT]
    toe_off_idx = jump_indices[DataIndicesExtraction.TOE_OFF]
    if highest_point_idx is None or toe_off_idx is None:
        return np.nan

    head_vertical_position = data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.VERTICAL)
    if head_vertical_position.empty:
        return np.nan
    return compute_norm(head_vertical_position.iloc[highest_point_idx] - head_vertical_position.iloc[toe_off_idx])


def _flight_time(data: Data, jump_indices: dict[DataIndicesExtraction, float]) -> np.float64:
    """
    Returns the duration of the jump based on the time stamps.
    This is the time traveled from toe-off to reception.
    """
    toe_off_idx = jump_indices[DataIndicesExtraction.TOE_OFF]
    reception_idx = jump_indices[DataIndicesExtraction.RECEPTION]
    if toe_off_idx is None or reception_idx is None:
        return np.nan

    time = data.time
    if time.empty:
        return np.nan
    return data.time.iloc[[toe_off_idx, reception_idx]].diff().values[1]
