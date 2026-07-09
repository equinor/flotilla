import { Icon, Typography } from '@equinor/eds-core-react'
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
import { InspectionData } from 'models/InspectionRecord'

interface InspectionDialogViewProps {
    selectedInspectionId: string
    inspectionData: InspectionData[]
}

export const InspectionTaskDialogView = ({ selectedInspectionId, inspectionData }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const [switchImageDirection, setSwitchImageDirection] = useState<number>(0)
    const { switchSelectedInspectionId } = useInspectionId()

    const inspectionIndex = inspectionData.findIndex((i) => i.inspectionId == selectedInspectionId)
    const currentInspection = inspectionData[inspectionIndex]

    const closeDialog = () => {
        switchSelectedInspectionId(undefined)
    }

    if (!currentInspection) {
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
            const nextTask = inspectionData.indexOf(currentInspection) + switchImageDirection
            if (nextTask >= 0 && nextTask < inspectionData.length) {
                switchSelectedInspectionId(inspectionData[nextTask].inspectionId)
            }
            setSwitchImageDirection(0)
        }
    })

    return (
        <StyledDialog open={true} isDismissable onClose={closeDialog}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Inspection report for task') + ' ' + (inspectionIndex + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={closeDialog}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <LargeDialogInspectionResult inspection={currentInspection} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installation.name}</Typography>
                            </StyledInfoContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{currentInspection.tag}</Typography>
                            </StyledInfoContent>
                            {currentInspection.inspectionDescription && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {currentInspection.inspectionDescription}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {currentInspection.createdAt && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(currentInspection.createdAt)}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                        </StyledBottomContent>
                    </div>
                    <HiddenOnSmallScreen>
                        <InspectionOverviewDialogView inspectionData={inspectionData} />
                    </HiddenOnSmallScreen>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}

export const InspectionDialogView = ({
    selectedInspectionId,
    inspectionData,
}: {
    selectedInspectionId: string | undefined
    inspectionData: InspectionData[]
}) => {
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const [switchImageDirection, setSwitchImageDirection] = useState<number>(0)
    const { switchSelectedInspectionId } = useInspectionId()

    const inspectionIndex = inspectionData.findIndex((t) => t.inspectionId == selectedInspectionId)
    const currentInspection = inspectionData[inspectionIndex]

    const closeDialog = () => {
        switchSelectedInspectionId(undefined)
    }

    if (!currentInspection) {
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
            const nextInspection = inspectionIndex + switchImageDirection
            if (nextInspection >= 0 && nextInspection < inspectionData.length) {
                switchSelectedInspectionId(inspectionData[nextInspection].inspectionId)
            }
            setSwitchImageDirection(0)
        }
    })

    return (
        <StyledDialog open={true} isDismissable onClose={closeDialog}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Inspection report for task') + ' ' + (inspectionIndex + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={closeDialog}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <LargeDialogInspectionResult inspection={currentInspection} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installation.name}</Typography>
                            </StyledInfoContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{currentInspection.tag}</Typography>
                            </StyledInfoContent>
                            {currentInspection.inspectionDescription && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {currentInspection.inspectionDescription}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {currentInspection.createdAt && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(currentInspection.createdAt)}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                        </StyledBottomContent>
                    </div>
                    <HiddenOnSmallScreen>
                        <InspectionOverviewDialogView inspectionData={inspectionData} />
                    </HiddenOnSmallScreen>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}
