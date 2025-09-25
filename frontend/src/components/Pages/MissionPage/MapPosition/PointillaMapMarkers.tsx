import { Task, TaskStatus } from 'models/Task'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import L from 'leaflet'
import robotPictogram from 'mediaAssets/onshore_drone.svg'
import { Pose } from 'models/Pose'

const robotIcon = L.icon({
    iconUrl: robotPictogram,
    iconSize: [20, 30],
    iconAnchor: [10, 15],
})

const orderTasksByDrawOrder = (tasks: Task[]) => {
    const isOngoing = (task: Task) => task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
    const sortedTasks = [...tasks].sort((a, b) => {
        if (a.status === TaskStatus.NotStarted && b.status === TaskStatus.NotStarted) return b.taskOrder - a.taskOrder
        else if (isOngoing(a)) return 1
        else if (isOngoing(b)) return -1
        else if (a.status === TaskStatus.NotStarted) return 1
        else if (b.status === TaskStatus.NotStarted) return -1
        return a.taskOrder - b.taskOrder
    })
    return sortedTasks
}

const getTaskMarker = (map: L.Map, task: Task) => {
    const color = getColorsFromTaskStatus(task.status)

    const taskMarker = L.circleMarker([task.robotPose.position.y, task.robotPose.position.x], {
        radius: 15,
        fillColor: color.fillColor,
        color: 'black',
        weight: 1,
        fillOpacity: 0.8,
    })
        .bindTooltip((task.taskOrder + 1).toString(), {
            permanent: true,
            direction: 'center',
            className: 'circleLabel',
        })
        .addTo(map)
    return taskMarker
}

export const getTaskMarkers = (map: L.Map, tasks: Task[]) => {
    return orderTasksByDrawOrder(tasks).map((task) => getTaskMarker(map, task))
}

const getRobotAuraMarker = (map: L.Map, robotPose: Pose) => {
    const msFromMinToMax = 2000
    const minPixelRadius = 15
    const maxPixelRadius = 18
    const currentAuraRadius =
        ((new Date().getTime() % msFromMinToMax) * (maxPixelRadius - minPixelRadius)) / msFromMinToMax + minPixelRadius

    const auraMarker = L.circleMarker([robotPose.position.y, robotPose.position.x], {
        radius: currentAuraRadius,
        fillColor: 'white',
        weight: 0,
        fillOpacity: 0.9,
    }).addTo(map)

    return auraMarker
}

export const getRobotMarker = (map: L.Map, robotPose: Pose) => {
    const auraMarker = getRobotAuraMarker(map, robotPose)
    const robotMarker = L.marker([robotPose.position.y, robotPose.position.x], {
        icon: robotIcon,
    }).addTo(map)

    return [robotMarker, auraMarker]
}
