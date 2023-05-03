import { tokens } from '@equinor/eds-tokens'
import { TaskStatus } from 'models/Task'

export const GetColorsFromTaskStatus = (taskStatus: TaskStatus) => {
    var fillColor = tokens.colors.ui.background__medium.hex
    var textColor = 'black'
    if (taskStatus === TaskStatus.NotStarted) {
        fillColor = tokens.colors.ui.background__info.hex
    } else if (taskStatus === TaskStatus.InProgress || taskStatus === TaskStatus.Paused) {
        fillColor = tokens.colors.interactive.primary__resting.hex
        textColor = 'white'
    }
    return { fillColor: fillColor, textColor: textColor }
}
