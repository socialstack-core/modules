import User from '../User';
import { useEffect, useRef } from 'react';

const MAX_STAGE_USERS = 6;

function emptyStage() {
	return <div className="huddle-chat__stage">
		<h2 className="huddle-chat__stage-message">
            {`Getting everything ready ...`}
		</h2>
	</div>;
}

function getStageLayout(n, fraction) {
    var firstRow = Math.ceil(n / fraction);

    if (firstRow == 1) {
        return [];
    }

    var remaining = n - firstRow;
    var layout = [firstRow];

    if (remaining <= firstRow) {
        layout.push(remaining);
    } else {
        var halfRemaining = Math.round(remaining / 2);
        layout.push(halfRemaining);
        layout.push(remaining - halfRemaining);
    }

    return layout;
}

function getStageLayouts(n) {
    var layouts = [[n]];
    var halfLayout = n > 2 ? getStageLayout(n, 2) : [];
    var thirdLayout = n > 4 ? getStageLayout(n, 3) : [];

    if (halfLayout.length) {
        layouts.push(halfLayout);
    }

    if (thirdLayout.length) {
        layouts.push(thirdLayout);
    }

    return layouts;
}

function getLayouts(total) {
    var layouts = getStageLayouts(total);
    var ratios = layouts.map(layout => {
        var x = Math.max(...layout);
        var y = layout.length;
        var lw = 16 * x;
        var lh = 9 * y;
        var pw = 16 * y;
        var ph = 9 * x;

        return total == 1 ? [[[lw, lh]]] : [[lw, lh], [pw, ph]];
    });

    return ratios.flat();
}

function getClosestRatio(ratios, target) {
    var closest = ratios.reduce(function (prev, curr) {
        var prevRatio = prev[0] / prev[1];
        var currRatio = curr[0] / curr[1];
        return (Math.abs(currRatio - target) < Math.abs(prevRatio - target) ? curr : prev);
    });

    return [closest[0] / 16, closest[1] / 9];
}

function updateAspectRatio(stage, stageLayouts) {

    if (!stage) {
        return;
    }

    var rect = stage.getBoundingClientRect();
    var ratio = rect.width / rect.height;

    for (var i = 2; i <= MAX_STAGE_USERS; i++) {
        stage.dataset['ratio' + i] = getClosestRatio(stageLayouts[i-2], ratio).join('-');
    }

}

export default function StageView(props) {
    var { users, huddleClient, isHosted, hostArrived, emptyHuddle, showDebugInfo } = props;
    const stageRef = useRef(null);

    var stageLayouts = [];

    for (var i = 2; i <= MAX_STAGE_USERS; i++) {
        stageLayouts.push(getLayouts(i));
    }
	
    useEffect(() => {
        var stage = stageRef.current;

        if (!stage) {
            return;
        }

        var stageObserver;

        stageObserver = new ResizeObserver(entries => {
            updateAspectRatio(stage, stageLayouts);
        });

        stageObserver.observe(stage);
        updateAspectRatio(stage, stageLayouts);

        return () => {
            stageObserver.unobserve(stage);
            stageObserver.disconnect();
        };

    });

    if (!users || !users.length) {
        return emptyStage();
    }

    return <ul className="huddle-chat__stage" data-users={users ? users.length : undefined} ref={stageRef}>
        {users.map(user => {
            return <User className="huddle-chat__stage-user" key={user.id} user={user} huddleClient={huddleClient} showDebugInfo={showDebugInfo} isStage />;
        })}
    </ul>;
}