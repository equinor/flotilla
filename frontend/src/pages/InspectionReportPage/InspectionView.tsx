import { Icon, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { Inspection, SensorType } from 'models/Inspection'
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
import { InspectionOverviewDialogView } from './ImageOverview'
import { useContext, useState } from 'react'
import { LargeDialogInspectionResult, TextAsImage } from './InspectionReportImage'
import { useInspectionId } from './SetInspectionIdHook'
import { InstallationContext } from 'components/Contexts/InstallationContext'

interface InspectionDialogViewProps {
    selectedInspectionId: string
    tasks: Task[]
}

const AcousticMetadataInfo = ({ inspection }: { inspection: Inspection }) => {
    const { TranslateText } = useLanguageContext()
    const metadata = inspection.acousticInspectionMetadata
    if (inspection.inspectionType !== SensorType.AcousticMeasurement || !metadata) {
        return null
    }
    return (
        <>
            <StyledInfoContent>
                <Typography variant="caption">{TranslateText('Frequency range') + ':'}</Typography>
                <Typography variant="body_short">
                    {metadata.frequencyFrom + ' - ' + metadata.frequencyTo + ' Hz'}
                </Typography>
            </StyledInfoContent>
            <StyledInfoContent>
                <Typography variant="caption">{TranslateText('SNR threshold') + ':'}</Typography>
                <Typography variant="body_short">{metadata.snrValueThreshold + ' dB'}</Typography>
            </StyledInfoContent>
            <StyledInfoContent>
                <Typography variant="caption">{TranslateText('Detection type') + ':'}</Typography>
                <Typography variant="body_short">{TranslateText(metadata.detectionType)}</Typography>
            </StyledInfoContent>
        </>
    )
}

export const InspectionDialogView = ({ selectedInspectionId, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const [switchImageDirection, setSwitchImageDirection] = useState<number>(0)
    const { switchSelectedInspectionId } = useInspectionId()

    const taskIndex = tasks.findIndex((t) => t.inspection.isarInspectionId == selectedInspectionId)
    const currentTask = tasks[taskIndex]

    const closeDialog = () => {
        switchSelectedInspectionId(undefined)
    }

    if (!currentTask) {
        return (
            <StyledDialog open={true} isDismissable onClose={closeDialog}>
                <StyledDialogContent>
                    <StyledDialogInspectionView>
                        <TextAsImage isLargeImage={true} text="No inspection could be found" />
                    </StyledDialogInspectionView>
                </StyledDialogContent>
            </StyledDialog>
        )
    }

    const successfullyCompletedTasks = tasks.filter((task) => task.status === 'Successful')

    document.addEventListener('keydown', (event) => {
        // Let a focused video handle arrow keys for seeking instead of switching inspection.
        if (event.target instanceof HTMLMediaElement) return
        if (event.code === 'ArrowLeft' && switchImageDirection !== -1) {
            setSwitchImageDirection(-1)
        } else if (event.code === 'ArrowRight' && switchImageDirection !== 1) {
            setSwitchImageDirection(1)
        }
    })

    document.addEventListener('keyup', (event) => {
        if (event.target instanceof HTMLMediaElement) return
        if (
            (event.code === 'ArrowLeft' && switchImageDirection === -1) ||
            (event.code === 'ArrowRight' && switchImageDirection === 1)
        ) {
            const nextTask = successfullyCompletedTasks.indexOf(currentTask) + switchImageDirection
            if (nextTask >= 0 && nextTask < successfullyCompletedTasks.length) {
                switchSelectedInspectionId(successfullyCompletedTasks[nextTask].inspection.isarInspectionId)
            }
            setSwitchImageDirection(0)
        }
    })

    return (
        <StyledDialog open={true} isDismissable onClose={closeDialog}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Inspection report for task') + ' ' + (taskIndex + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={closeDialog}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <LargeDialogInspectionResult task={currentTask} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installation.name}</Typography>
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
                                    <Typography variant="body_short">{formatDateTime(currentTask.endTime)}</Typography>
                                </StyledInfoContent>
                            )}
                            <AcousticMetadataInfo inspection={currentTask.inspection} />
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
