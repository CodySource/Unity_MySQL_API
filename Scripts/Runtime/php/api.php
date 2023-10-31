<?php

//	Set error handler
set_error_handler('ErrorHandle');

//	Import Creds & Keys
require_once 'creds.php';
require_once 'keys.php';

//	Declare version & acquire necessary data
const version = '1-0-0';
$providedVersion = isset($_SERVER['HTTP_API_VERSION']) ? $_SERVER['HTTP_API_VERSION'] : '';
$providedKey = $_SERVER['HTTP_X_API_KEY']; $_SERVER['HTTP_X_API_KEY'] : '';

//	Ensure appropriate headers
if ($providedVersion != version || !array_key_exists($providedKey,keys))
{
	trigger_error('Mismatched api version or invalid api key supplied.');
}

//	Establish connection
$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
if ($mysqli->connect_errno) {trigger_error($mysqli->connect_error);}

switch ($_SERVER['REQUEST_METHOD']) {
	case 'GET':
		$where = (isset($_GET['id']))? ' WHERE id = "'.$_GET['id'].'"' : '';
		GET($where);
		break;	
	case 'POST':
		//	Pass in id = -1 to increment entries instead of assigning them to an id
		$data = _ScreenPostData();
		POST($data);
		break;
	case 'PUT':
		$data = _ScreenPostData();
		$data = (object)array_merge((array)$data,array('WHERE'=>' WHERE id = "$data->id"'));
		UPDATE($data);
		break;
	default:
		trigger_error('Unsupported request method.');
		break;
}

//	Returns all data in the table or a specific entry if an 'id' is provided
function GET($where)
{
	global $mysqli, $providedKey, $where;
	//	Prepare & Execute Statment
	$stmt = $mysqli->prepare('SELECT id, value FROM '. keys[$providedKey] . $where);
	$stmt->execute();
	//	Parse results
	$stmt->bind_result($key,$val);
	$output = (object)array();
	while ($stmt->fetch()) {$output->$key=json_decode($val);}
	$stmt->close();
	$mysqli->close();
	Respond($output);
}

//	Screens the id and value post fields 
function _ScreenPostData()
{
	if (!isset($_POST['id']) || !isset($_POST['value']))
		trigger_error('Invalid submission -- Missing id and/or value.');
	$id = $_POST['id'];
	$value = json_decode($_POST['value']);
	if ($value == null)
		trigger_error('Invalid submission -- Value must be in json formatting.');
	$value=(object)array_merge((array)$value,array('_timestamp'=>date(DATE_RFC3339)));
	return (object)array('id'=>$id,'value'=>$value);
}

//	Push new content to the database
function POST($data)
{
	global $mysqli, $providedKey, $where;
	$table = keys[$providedKey];
	if (!$mysqli->query("CREATE TABLE IF NOT EXISTS $table (id VARCHAR(1023) PRIMARY KEY, value TEXT);")) o(null, $mysqli->error);
	$q = $mysqli->prepare("INSERT INTO $table (id, value) VALUES('".(($data->id != -1)? $$data->id : $result->num_rows)."', ?)");
	$q->bind_param('s', $data->value);
	if (!$q->execute()) o(null, $mysqli->error);
	o(date(DATE_RFC3339), null);
}

//	Update existing database content
function UPDATE($data)
{
	global $mysqli, $providedKey, $where;
	$table = keys[$providedKey];
	if (!$mysqli->query("CREATE TABLE IF NOT EXISTS $table (id VARCHAR(1023) PRIMARY KEY, value TEXT);")) o(null, $mysqli->error);
	$result = $mysqli->query("SELECT * FROM $table WHERE id='$data->id'");
	if ($result->num_rows == 0) trigger_error('Invalid submission -- ID not found.');
	$q = $mysqli->prepare("UPDATE $table SET id=? WHERE value='$data->value'");
	$q->bind_param('s', $data->id);
	if (!$q->execute()) o(null, $mysqli->error);
	o(date(DATE_RFC3339), null);
}

//	Define return funtions
function Respond($v,$e=null){die(json_encode(($v != null)?$v:(object)array('error'=>$e)));}

///	Used to handle any errors
function ErrorHandle($errNo,$errStr,$errFile,$errLine){$msg="$errStr in $errFile on line $errLine";error_log($msg);Respond(null,$errStr);}

?>