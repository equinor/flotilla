import { Icon, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import {
    HiddenOnSmallScreen,
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledInfoContent,
} from './InspectionStyles'
import { InspectionOverviewDialogView } from './InspectionOverview'
import { useState } from 'react'
import { LargeDialogInspectionImage, LargeImageErrorPlaceholder } from './InspectionReportImage'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { useInspectionId } from './SetInspectionIdHook'

interface InspectionDialogViewProps {
    selectedInspectionId: string
    tasks: Task[]
}

export const InspectionDialogView = ({ selectedInspectionId, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationName } = useAssetContext()
    const [switchImageDirection, setSwitchImageDirection] = useState<number>(0)
    const { switchSelectedInspectionId } = useInspectionId()

    const currentTask = tasks.find((t) => t.inspection.id == selectedInspectionId)

    const closeDialog = () => {
        switchSelectedInspectionId(undefined)
    }

    if (!currentTask) {
        return (
            <StyledDialog open={true} isDismissable onClose={closeDialog}>
                <StyledDialogContent>
                    <StyledDialogInspectionView>
                        <LargeImageErrorPlaceholder errorMessage="No inspection could be found" />
                    </StyledDialogInspectionView>
                </StyledDialogContent>
            </StyledDialog>
        )
    }

    const successfullyCompletedTasks = tasks.filter((task) => task.status === 'Successful')

    document.addEventListener('keydown', (event) => {
        if (event.code === 'ArrowLeft' && switchImageDirection !== -1) {
            setSwitchImageDirection(-1)
        } else if (event.code === 'ArrowRight' && switchImageDirection !== 1) {
            setSwitchImageDirection(1)
        }
    })

    document.addEventListener('keyup', (event) => {
        if (
            (event.code === 'ArrowLeft' && switchImageDirection === -1) ||
            (event.code === 'ArrowRight' && switchImageDirection === 1)
        ) {
            const nextTask = successfullyCompletedTasks.indexOf(currentTask) + switchImageDirection
            if (nextTask >= 0 && nextTask < successfullyCompletedTasks.length) {
                switchSelectedInspectionId(successfullyCompletedTasks[nextTask].inspection.id)
            }
            setSwitchImageDirection(0)
        }
    })

    return (
        <StyledDialog open={true} isDismissable onClose={closeDialog}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Inspection report for task') + ' ' + (currentTask.taskOrder + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={closeDialog}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <LargeDialogInspectionImage task={currentTask} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installationName}</Typography>
                            </StyledInfoContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{currentTask.tagId}</Typography>
                            </StyledInfoContent>
                            {currentTask.description && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">{currentTask.description}</Typography>
                                </StyledInfoContent>
                            )}
                            {currentTask.endTime && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(currentTask.endTime, 'dd.MM.yy - HH:mm')}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                        </StyledBottomContent>
                    </div>
                    <HiddenOnSmallScreen>
                        <InspectionOverviewDialogView tasks={tasks} />
                    </HiddenOnSmallScreen>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}
